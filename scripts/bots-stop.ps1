# Stops a bot run early: leaves a "stop" file in its folder under bot-runs/, and the
# Overseer (game/bots/overseer) asks each bot to quit cleanly, then writes the run's
# report as usual. The newest run unless one is named (a soak, while a shorter run was
# started beside it).
#
#   .\scripts\bots-stop.ps1
#   .\scripts\bots-stop.ps1 -Run 20260929-011607

param(
    [string]$Run = ""
)

$root = Split-Path $PSScriptRoot -Parent
$runs = Join-Path $root "bot-runs"

if (-not (Test-Path $runs)) {
    Write-Host "No bot runs."
    exit 0
}

if ($Run -ne "") {
    $folder = Get-Item (Join-Path $runs $Run) -ErrorAction SilentlyContinue
} else {
    $folder = Get-ChildItem $runs -Directory | Sort-Object Name | Select-Object -Last 1
}

if ($null -eq $folder) {
    Write-Host "No bot run $Run."
    exit 1
}

Set-Content -Path (Join-Path $folder.FullName "stop") -Value "stop"
Write-Host "Asked the bots in $($folder.Name) to stop."
