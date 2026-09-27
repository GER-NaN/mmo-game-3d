# Builds the docs wiki from docs/ and serves it at http://localhost:8000.
#
#   .\scripts\wiki-publish.ps1              always
#   .\scripts\wiki-publish.ps1 -IfChanged   only when docs/ or tools/wiki changed since
#                                          the last publish (client-up.ps1 uses this)
#
# The site is built by Material for MkDocs in a container (tools/wiki), into
# tools/wiki/site, which the mmo3d-wiki container serves. Edit a doc, run this, reload
# the page. Broken links are listed; they do not stop the publish.
param(
    [switch]$IfChanged
)

$root = Split-Path $PSScriptRoot -Parent
$started = Get-Date
$siteIndex = Join-Path $root "tools\wiki\site\index.html"

if ($IfChanged -and (Test-Path $siteIndex)) {
    $builtAt = (Get-Item $siteIndex).LastWriteTime
    $sources = @(Get-ChildItem (Join-Path $root "docs"), (Join-Path $root "tools\wiki") -Recurse -File -Force |
        Where-Object { $_.FullName -notlike "*\tools\wiki\site\*" -and $_.LastWriteTime -gt $builtAt })

    if ($sources.Count -eq 0) {
        exit 0
    }
}

docker build -q -t mmo3d-wiki-builder (Join-Path $root "tools\wiki") | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Could not build the wiki builder image. Is Docker running?"
    exit 1
}

# The builder prints a long notice about MkDocs 2.0 on every run; only the build's own
# lines are shown.
$output = docker run --rm -v "${root}:/repo" -w /repo/tools/wiki mmo3d-wiki-builder build --clean 2>&1
$built = $LASTEXITCODE -eq 0
$output | ForEach-Object { "$_" } | Where-Object { $_ -match "^(WARNING|ERROR)" } | ForEach-Object { Write-Host $_ }

if (-not $built) {
    Write-Host "The wiki did not build; the site is unchanged."
    exit 1
}

docker compose -f (Join-Path $root "docker\docker-compose.yml") up -d wiki 2>&1 | Out-Null
$seconds = [Math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host "Wiki published in $seconds s: http://localhost:8000"
