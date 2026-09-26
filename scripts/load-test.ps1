# Load test: a headless server that prints its numbers, and bot clients spread over
# several processes (one process holds about 50 bots before it slows to a crawl). The
# bots wander the town, pick things up and talk, and every one sees every other, which
# is the worst case for a zone.
#
#   .\scripts\load-test.ps1 -Bots 100 -Seconds 90
#
# The server's lines ("Stats: ...") go to load-test\server.log under the system temp
# folder, one per -StatsEvery seconds: players, frame and physics time, traffic out and
# in. Each bot process logs to load-test\bots-N.log. Needs Postgres, like the server.
# -NoDiagnostics runs the server without its logs and traces, to see what they cost;
# -LogPackets runs it with the packet log as well. -Scenario makes every bot keep doing
# one thing instead of wandering (load-phone, load-defense, load-taxi, load-chat; see
# game/dev/LoadBot.cs); the server is started with --dev-scenarios for it.
param(
    [int]$Bots = 50,
    [int]$PerProcess = 50,
    [int]$Seconds = 90,
    [int]$StatsEvery = 10,
    [switch]$NoDiagnostics,
    [switch]$LogPackets,
    [string]$Scenario = "",
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent
$logs = Join-Path ([System.IO.Path]::GetTempPath()) "load-test"
New-Item -ItemType Directory -Force $logs | Out-Null

dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$serverLog = Join-Path $logs "server.log"
$serverArgs = @("--headless", "--path", "`"$root`"", "--", "--server", "--stats-every", $StatsEvery, "--max-players", ($Bots + 50))

if ($NoDiagnostics) {
    $serverArgs += @("--diagnostics", "off")
}

if ($LogPackets) {
    $serverArgs += "--log-packets"
}

if ($Scenario -ne "") {
    $serverArgs += "--dev-scenarios"
}

$server = Start-Process $Godot -ArgumentList $serverArgs `
    -RedirectStandardOutput $serverLog -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 7

$clients = @()

for ($first = 0; $first -lt $Bots; $first += $PerProcess) {
    $count = [Math]::Min($PerProcess, $Bots - $first)
    $log = Join-Path $logs ("bots-" + $first + ".log")
    $botArgs = @("--headless", "--path", "`"$root`"", "--", "--load-test", $count, "--load-first", $first)

    if ($Scenario -ne "") {
        $botArgs += @("--load-scenario", $Scenario)
    }

    $clients += Start-Process $Godot -ArgumentList $botArgs `
        -RedirectStandardOutput $log -WindowStyle Hidden -PassThru
}

Write-Host "Running $Bots bots in $($clients.Count) processes for $Seconds seconds; logs in $logs"
Start-Sleep -Seconds $Seconds

& (Join-Path $PSScriptRoot "server-stop.ps1") | Out-Null

foreach ($client in $clients) {
    Stop-Process -Id $client.Id -ErrorAction SilentlyContinue
}

Get-Content $serverLog | Select-String "Stats:" | Select-Object -Last 4
