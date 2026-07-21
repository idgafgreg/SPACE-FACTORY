<#
.SYNOPSIS
    Install this repo's git hooks into .git/hooks.

.DESCRIPTION
    Copies each file in tools/hooks/ into .git/hooks/.

    Deliberately does NOT set core.hooksPath: Git LFS installs post-checkout,
    post-commit, post-merge and pre-push into .git/hooks, and pointing hooksPath
    elsewhere would silently disable all four. Copying leaves LFS untouched.

    Safe to re-run. Refuses to overwrite a hook that was not installed from
    tools/hooks unless -Force is given.
#>
param([switch]$Force)

$ErrorActionPreference = 'Stop'

$root     = git rev-parse --show-toplevel
$src      = Join-Path $root 'tools/hooks'
$dstDir   = Join-Path $root '.git/hooks'
$marker   = 'tools/install-hooks.ps1'

if (-not (Test-Path $src))    { Write-Error "No tools/hooks directory at $src"; exit 1 }
if (-not (Test-Path $dstDir)) { Write-Error "No .git/hooks directory at $dstDir"; exit 1 }

foreach ($hook in Get-ChildItem $src -File) {
    $dst = Join-Path $dstDir $hook.Name

    if ((Test-Path $dst) -and -not $Force) {
        $existing = Get-Content $dst -Raw
        if ($existing -notmatch [regex]::Escape($marker)) {
            Write-Warning ("{0} already exists and was not installed by this script - skipping. Use -Force to overwrite." -f $hook.Name)
            continue
        }
    }

    # Stamp the marker so a re-run can tell its own output apart from a hook
    # someone else (or another tool) placed there.
    $body = Get-Content $hook.FullName -Raw
    $body = $body -replace '(?m)^# Install: .*$', "# Install: $marker  (do not edit .git/hooks directly - edit tools/hooks/ and re-run)"

    # Hooks are run through git's bundled sh; LF endings are required.
    [System.IO.File]::WriteAllText($dst, ($body -replace "`r`n", "`n"), (New-Object System.Text.UTF8Encoding($false)))
    Write-Output ("installed  .git/hooks/{0}" -f $hook.Name)
}

Write-Output ""
Write-Output "Done. Existing Git LFS hooks were left alone."
