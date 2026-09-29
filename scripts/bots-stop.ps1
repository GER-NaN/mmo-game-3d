# Stops the newest bot run early: leaves a "stop" file in its folder under bot-runs/,
# and the Overseer (game/bots/overseer) asks each bot to quit cleanly, then writes the
# run's report as usual.
#
#   .\scripts\bots-stop.ps1

$root = Split-Path $PSScriptRoot -Parent
$runs = Join-Path $root "bot-runs"

if (-not (Test-Path $runs)) {
    Write-Host "No bot runs."
    exit 0
}

$newest = Get-ChildItem $runs -Directory | Sort-Object Name | Select-Object -Last 1

if ($null -eq $newest) {
    Write-Host "No bot runs."
    exit 0
}

Set-Content -Path (Join-Path $newest.FullName "stop") -Value "stop"
Write-Host "Asked the bots in $($newest.Name) to stop."
