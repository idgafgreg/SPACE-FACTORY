#Requires -Version 5.1
<#
.SYNOPSIS
  Rebuild lore/assets-wishlist.csv from wishlist-paywalled.md + daily assets-tools.md free leads.

.DESCRIPTION
  Run after any lore asset/wishlist update (daily research agents should call this).
  Then refresh the Google Sheet:
    - If using IMPORTDATA on the raw GitHub CSV URL: wait for push, sheet auto-refreshes.
    - Otherwise: File > Import > Upload this CSV (Replace current sheet).

.EXAMPLE
  powershell -File lore/sync-assets-sheet.ps1
#>

$ErrorActionPreference = 'Stop'
$loreRoot = $PSScriptRoot
$wishlistPath = Join-Path $loreRoot 'wishlist-paywalled.md'
$outPath = Join-Path $loreRoot 'assets-wishlist.csv'

function Escape-CsvField([string]$value) {
    if ($null -eq $value) { return '' }
    $v = $value.Trim()
    if ($v -match '[,"\r\n]') {
        return '"' + ($v -replace '"', '""') + '"'
    }
    return $v
}

function Parse-WishlistRow([string]$line) {
    # | Priority | Item | Est. price | Link | Why | Notes |
    if ($line -notmatch '^\|') { return $null }
    if ($line -match '^\|\s*-+\s*\|') { return $null }
    if ($line -match '^\|\s*Priority\s*\|') { return $null }

    $cells = $line.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    if ($cells.Count -lt 6) { return $null }

    $priority = $cells[0]
    $item = $cells[1] -replace '\*([^*]+)\*', '$1'
    $price = $cells[2]
    $link = $cells[3]
    $why = $cells[4]
    $notes = $cells[5]

    # Extract first http(s) URL if the link cell has extra text
    $url = $link
    if ($link -match '(https?://\S+)') { $url = $Matches[1].TrimEnd(')') }

    # Infer first-seen from notes when present
    $firstSeen = ''
    if ($notes -match 'Added\s+(\d{4}-\d{2}-\d{2})') { $firstSeen = $Matches[1] }

    return [pscustomobject]@{
        Priority = $priority
        Cost     = 'Paid'
        Item     = $item
        Price    = $price
        Link     = $url
        Why      = $why
        Source   = 'wishlist-paywalled.md'
        FirstSeen = $firstSeen
        Status   = 'Wishlist'
        Notes    = $notes
    }
}

function Get-FreeLeads {
    $leads = New-Object System.Collections.Generic.List[object]
    $assetFiles = Get-ChildItem -Path $loreRoot -Directory |
        Where-Object { $_.Name -match '^\d{4}-\d{2}-\d{2}$' } |
        ForEach-Object { Join-Path $_.FullName 'assets-tools.md' } |
        Where-Object { Test-Path $_ }

    $sectionRx = [regex]::new(
        '##\s*Free\s*/\s*open leads\s*(.*?)(?=##\s*Paid|\z)',
        [System.Text.RegularExpressions.RegexOptions]::Singleline -bor
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
    )
    $headingRx = [regex]::new(
        '^###\s+(.+)$',
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )

    foreach ($file in $assetFiles) {
        $date = Split-Path (Split-Path $file -Parent) -Leaf
        $rel = "$date/assets-tools.md"
        $text = Get-Content -LiteralPath $file -Raw -Encoding UTF8
        $secMatch = $sectionRx.Match($text)
        if (-not $secMatch.Success) { continue }
        $section = $secMatch.Groups[1].Value

        $headingMatches = $headingRx.Matches($section)
        for ($i = 0; $i -lt $headingMatches.Count; $i++) {
            $hm = $headingMatches[$i]
            $start = $hm.Index
            $end = if ($i + 1 -lt $headingMatches.Count) { $headingMatches[$i + 1].Index } else { $section.Length }
            $block = $section.Substring($start, $end - $start)

            $title = $hm.Groups[1].Value.Trim()
            # Drop trailing "— Unity Asset Store..." fluff after dash variants
            if ($title -match '^(.+?)\s+[\u2014\u2013\-]\s+') { $title = $Matches[1].Trim() }

            $url = ''
            if ($block -match '\*\*URL:\*\*\s*(https?://\S+)') { $url = $Matches[1].Trim().TrimEnd(').,') }
            elseif ($block -match '\*\*Repo:\*\*\s*(https?://\S+)') { $url = $Matches[1].Trim().TrimEnd(').,') }
            elseif ($block -match '(https?://(?:assetstore\.unity\.com|github\.com)[^\s\)]+)') { $url = $Matches[1].Trim() }

            $fit = ''
            if ($block -match '\*\*Fit:\*\*\s*(.+)') { $fit = ($Matches[1] -split "`r?`n")[0].Trim() }

            $action = ''
            if ($block -match '\*\*Action:\*\*\s*(.+)') { $action = ($Matches[1] -split "`r?`n")[0].Trim() }

            if ([string]::IsNullOrWhiteSpace($title) -or [string]::IsNullOrWhiteSpace($url)) { continue }

            $leads.Add([pscustomobject]@{
                Priority  = 'Free'
                Cost      = 'Free'
                Item      = $title
                Price     = 'Free'
                Link      = $url
                Why       = $fit
                Source    = $rel
                FirstSeen = $date
                Status    = 'Evaluate'
                Notes     = $action
            }) | Out-Null
        }
    }

    # Dedupe free leads by Link (keep earliest FirstSeen)
    if ($leads.Count -eq 0) { return ,@() }

    $result = New-Object System.Collections.ArrayList
    $seenUrls = @{}
    foreach ($lead in ($leads | Sort-Object FirstSeen, Item)) {
        $key = [string]$lead.Link
        if ([string]::IsNullOrWhiteSpace($key)) { continue }
        if ($seenUrls.ContainsKey($key.ToLowerInvariant())) { continue }
        $seenUrls[$key.ToLowerInvariant()] = $true
        [void]$result.Add($lead)
    }
    return ,$result.ToArray()
}

# --- Build rows ---
if (-not (Test-Path -LiteralPath $wishlistPath)) {
    throw "Missing wishlist: $wishlistPath"
}

$rows = @()
Get-Content -LiteralPath $wishlistPath -Encoding UTF8 | ForEach-Object {
    $parsed = Parse-WishlistRow $_
    if ($null -ne $parsed) { $rows += $parsed }
}

$free = @(Get-FreeLeads)
# Skip free leads whose URL already appears on the paid wishlist
$paidUrls = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($r in $rows) { [void]$paidUrls.Add($r.Link) }
foreach ($f in $free) {
    if (-not $paidUrls.Contains($f.Link)) { $rows += $f }
}

# Priority sort: High > Medium > Low > Free
$rank = @{ High = 0; Medium = 1; Low = 2; Free = 3 }
$rows = $rows | Sort-Object @{ Expression = { if ($rank.ContainsKey($_.Priority)) { $rank[$_.Priority] } else { 9 } } }, Item

$header = 'Priority,Cost,Item,Est. price,Link,Why it fits SPACE FACTORY,Source,First seen,Status,Notes'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add($header) | Out-Null

foreach ($r in $rows) {
    $line = @(
        (Escape-CsvField $r.Priority),
        (Escape-CsvField $r.Cost),
        (Escape-CsvField $r.Item),
        (Escape-CsvField $r.Price),
        (Escape-CsvField $r.Link),
        (Escape-CsvField $r.Why),
        (Escape-CsvField $r.Source),
        (Escape-CsvField $r.FirstSeen),
        (Escape-CsvField $r.Status),
        (Escape-CsvField $r.Notes)
    ) -join ','
    $lines.Add($line) | Out-Null
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllLines($outPath, $lines, $utf8NoBom)

# Local clickable HTML (works offline; regenerate with this script)
function Html-Encode([string]$s) {
    if ($null -eq $s) { return '' }
    return [System.Net.WebUtility]::HtmlEncode($s)
}

$htmlPath = Join-Path $loreRoot 'assets-wishlist.html'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<!DOCTYPE html>')
[void]$sb.AppendLine('<html lang="en"><head><meta charset="utf-8">')
[void]$sb.AppendLine('<title>SPACE FACTORY — Lore asset wishlist</title>')
[void]$sb.AppendLine('<style>')
[void]$sb.AppendLine('body{font:14px/1.4 system-ui,Segoe UI,sans-serif;margin:24px;background:#111;color:#e8e8e8}')
[void]$sb.AppendLine('h1{font-size:20px;margin:0 0 8px} .meta{color:#999;margin-bottom:16px}')
[void]$sb.AppendLine('table{border-collapse:collapse;width:100%} th,td{border:1px solid #333;padding:8px 10px;vertical-align:top}')
[void]$sb.AppendLine('th{background:#1c1c1c;text-align:left;position:sticky;top:0} tr:nth-child(even){background:#161616}')
[void]$sb.AppendLine('a{color:#8ec7ff} .High{color:#ff8e8e} .Medium{color:#ffd28e} .Low{color:#bbb} .Free{color:#8effb0}')
[void]$sb.AppendLine('</style></head><body>')
[void]$sb.AppendLine('<h1>SPACE FACTORY — Lore asset wishlist</h1>')
[void]$sb.AppendLine("<p class=`"meta`">Generated $(Get-Date -Format 'yyyy-MM-dd HH:mm') · Source: wishlist-paywalled.md + assets-tools.md · Regenerate: sync-assets-sheet.ps1</p>")
[void]$sb.AppendLine('<table><thead><tr><th>Priority</th><th>Cost</th><th>Item</th><th>Price</th><th>Link</th><th>Why</th><th>Status</th><th>Notes</th></tr></thead><tbody>')
foreach ($r in $rows) {
    $pri = Html-Encode $r.Priority
    $linkCell = if ($r.Link -match '^https?://') {
        "<a href=`"$(Html-Encode $r.Link)`" target=`"_blank`" rel=`"noopener`">Open</a>"
    } else { (Html-Encode $r.Link) }
    [void]$sb.AppendLine("<tr><td class=`"$pri`">$pri</td><td>$(Html-Encode $r.Cost)</td><td>$(Html-Encode $r.Item)</td><td>$(Html-Encode $r.Price)</td><td>$linkCell</td><td>$(Html-Encode $r.Why)</td><td>$(Html-Encode $r.Status)</td><td>$(Html-Encode $r.Notes)</td></tr>")
}
[void]$sb.AppendLine('</tbody></table></body></html>')
[System.IO.File]::WriteAllText($htmlPath, $sb.ToString(), $utf8NoBom)

Write-Host "Wrote $($rows.Count) rows -> $outPath"
Write-Host "Wrote clickable HTML -> $htmlPath"
Write-Host "Paid: $(($rows | Where-Object Cost -eq 'Paid').Count) | Free: $(($rows | Where-Object Cost -eq 'Free').Count)"
Write-Host ""
Write-Host "Google Sheet refresh options:"
Write-Host "  1) IMPORTDATA (public repo): =IMPORTDATA(`"https://raw.githubusercontent.com/idgafgreg/SPACE-FACTORY/main/lore/assets-wishlist.csv`")"
Write-Host "  2) Manual: Google Sheets -> File -> Import -> Upload lore/assets-wishlist.csv -> Replace"
