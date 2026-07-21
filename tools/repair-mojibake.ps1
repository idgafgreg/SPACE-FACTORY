<#
.SYNOPSIS
    Detect and repair mojibake (mis-encoded punctuation) in this repo's text files.

.DESCRIPTION
    On 2026-07-20 BACKLOG.md was found with 1108 corrupt characters: every em dash,
    arrow and comparison operator had been through one to three rounds of
    UTF-8 -> Windows-1252 -> UTF-8 re-encoding, leaving runs of up to 19 garbage
    characters and making 122 lines unreadable in any diff. Cause is a tool writing
    the file with the legacy Windows-1252 default instead of UTF-8.

    A blanket cp1252<->UTF-8 round-trip does NOT fix this safely. The documents are
    mixed: older sections are mangled (at varying depths) while newer sections are
    clean UTF-8 containing characters cp1252 cannot represent at all. Round-tripping
    the whole file destroys the clean parts.

    Instead this script mangles each candidate character FORWARD 1..3 times and uses
    the results as an exact search-and-replace table. Only sequences that can be
    positively accounted for are touched; anything unrecognised is reported, never
    guessed at.

.EXAMPLE
    # Report what is wrong, change nothing (default)
    ./tools/repair-mojibake.ps1 -All

.EXAMPLE
    # Repair one file
    ./tools/repair-mojibake.ps1 -Path BACKLOG.md -Apply

.EXAMPLE
    # Used by .git/hooks/pre-commit: check staged content, exit 1 if mojibake found
    ./tools/repair-mojibake.ps1 -Check -Staged
#>
[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter(ParameterSetName = 'Path')][string]$Path,
    [Parameter(ParameterSetName = 'All')][switch]$All,
    [switch]$Staged,
    [switch]$Check,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

$cp1252 = [System.Text.Encoding]::GetEncoding(1252)
$utf8   = [System.Text.Encoding]::UTF8

function Mangle-Once([string]$s) {
    return $cp1252.GetString($utf8.GetBytes($s))
}

# Characters this project's docs actually use. Extend if a new one shows up as
# "unaccounted" in the output below - do not guess at replacements by hand.
$candidates = @(
    [char]0x2014,  # em dash
    [char]0x2013,  # en dash
    [char]0x2212,  # minus sign
    [char]0x2192,  # right arrow
    [char]0x2190,  # left arrow
    [char]0x2248,  # almost equal
    [char]0x2265,  # greater or equal
    [char]0x2264,  # less or equal
    [char]0x00D7,  # multiplication sign
    [char]0x00B0,  # degree
    [char]0x2026,  # ellipsis
    [char]0x2018, [char]0x2019,  # curly single quotes
    [char]0x201C, [char]0x201D,  # curly double quotes
    [char]0x2022,  # bullet
    [char]0x00A0,  # non-breaking space
    [char]0x00B7   # middle dot
)

# Longest first, so a 3x-mangled run is matched before its 1x prefix.
$map = foreach ($c in $candidates) {
    $s = [string]$c
    for ($depth = 1; $depth -le 3; $depth++) {
        $s = Mangle-Once $s
        [pscustomobject]@{ From = $s; To = [string]$c; Depth = $depth; Len = $s.Length }
    }
}
$map = $map | Where-Object { $_.From -ne $_.To } | Sort-Object Len -Descending

# Characters that only appear inside mis-decoded UTF-8 in these documents.
$suspicious = '[ÃÂÆ]'

function Repair-Text([string]$text) {
    $out     = $text
    $applied = @()
    foreach ($e in $map) {
        $n = ([regex]::Matches($out, [regex]::Escape($e.From))).Count
        if ($n -gt 0) {
            $out = $out.Replace($e.From, $e.To)
            $applied += "{0,5}x  depth {1}  ->  U+{2:X4}" -f $n, $e.Depth, [int][char]$e.To
        }
    }
    return [pscustomobject]@{
        Text        = $out
        Applied     = $applied
        Unaccounted = ([regex]::Matches($out, $suspicious)).Count
    }
}

# ---- Build the file list -------------------------------------------------

$targets = @()
if ($Staged) {
    $targets = git diff --cached --name-only --diff-filter=ACM |
        Where-Object { $_ -match '\.(md|txt|mdc)$' }
} elseif ($All) {
    $targets = git ls-files -- '*.md' '*.txt' '*.mdc'
} elseif ($Path) {
    $targets = @($Path)
} else {
    Write-Error "Specify -Path <file>, -All, or -Staged."
    exit 2
}

# ---- Scan ----------------------------------------------------------------

$hits = @()
foreach ($file in $targets) {
    if ($Staged) {
        # Read the STAGED blob, not the working tree - that is what is about to
        # be committed.
        #
        # This must NOT go through a PowerShell pipe: piping git's output decodes
        # it using the console encoding, which re-mangles the exact characters we
        # are looking for (the check still fires, but the report is wrong and the
        # replacement map cannot match). Redirect through cmd, which is byte-exact,
        # then decode as UTF-8 ourselves.
        $tmp = [System.IO.Path]::GetTempFileName()
        try {
            cmd /c "git show `":$file`" > `"$tmp`"" 2>$null
            if (-not (Test-Path $tmp)) { continue }
            $text = [System.IO.File]::ReadAllText($tmp, $utf8)
        } finally {
            Remove-Item $tmp -Force -ErrorAction SilentlyContinue
        }
        if ([string]::IsNullOrEmpty($text)) { continue }
    } else {
        if (-not (Test-Path $file)) { continue }
        $text = [System.IO.File]::ReadAllText((Resolve-Path $file), $utf8)
    }

    $before = ([regex]::Matches($text, $suspicious)).Count
    if ($before -eq 0) { continue }

    $r = Repair-Text $text
    $hits += [pscustomobject]@{
        File        = $file
        Corrupt     = $before
        Applied     = $r.Applied
        Unaccounted = $r.Unaccounted
        Text        = $r.Text
    }
}

# ---- Report / act --------------------------------------------------------

if ($hits.Count -eq 0) {
    if (-not $Check) { Write-Output "No mojibake found." }
    exit 0
}

foreach ($h in $hits) {
    Write-Output ""
    Write-Output ("{0}  -  {1} corrupt characters" -f $h.File, $h.Corrupt)
    $h.Applied | ForEach-Object { Write-Output ("    " + $_) }
    if ($h.Unaccounted -gt 0) {
        Write-Output ("    WARNING: {0} characters not accounted for by the map." -f $h.Unaccounted)
        Write-Output  "    Add the missing character to the candidates list in this script"
        Write-Output  "    rather than hand-editing the file."
    }
}

if ($Check) {
    Write-Output ""
    Write-Output "Mojibake detected. Repair with:"
    Write-Output "    pwsh -File tools/repair-mojibake.ps1 -All -Apply"
    exit 1
}

if ($Apply) {
    foreach ($h in $hits) {
        if ($Staged) { Write-Output "Skipping write for staged-only scan: $($h.File)"; continue }
        [System.IO.File]::WriteAllText(
            (Resolve-Path $h.File), $h.Text, (New-Object System.Text.UTF8Encoding($false)))
        Write-Output ("repaired  " + $h.File)
    }
    Write-Output ""
    Write-Output "Verify before committing: line count must be unchanged, and each changed"
    Write-Output "line must differ only by the mapped characters."
} else {
    Write-Output ""
    Write-Output "(dry run - pass -Apply to write)"
}
exit 0
