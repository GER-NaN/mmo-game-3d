# Runs the dev test scenarios: a headless server on its own port that allows them, then
# one headless client per scenario. The server sets each player up for its test (where
# they stand, what they carry, what is going on); the client tests the feature through
# input and prints PASS or FAIL. Each scenario must end within a minute.
#
#   .\scripts\scenario-test.ps1                        every scenario
#   .\scripts\scenario-test.ps1 -Scenarios defense,subway
#
# Scenarios are in game/server/ServerScenarios.cs (setup) and game/dev/ScenarioDriver.cs
# (the test). The server needs Postgres with the mmo3d database, as for server-up.ps1.
# Each run is a new player ("Test <name>"), so nothing from an earlier run is in the way.
param(
    [string[]]$Scenarios = @("cracker", "rootkit", "defense", "cameras", "subway", "book", "workbench", "college", "meadows", "hills", "lights", "taxi", "taxi-ride", "taxi-relog", "fix", "garden", "shop", "registrar", "too-dear", "phone-dead", "door-exit", "gap", "drop-wall"),
    [int]$Port = 7071,
    [int]$TimeoutSeconds = 60,
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent
$logs = Join-Path $env:TEMP "mmo-game-3d-scenarios"
New-Item -ItemType Directory -Force $logs | Out-Null

dotnet build (Join-Path $root "mmo-game-3d.sln") | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit 1
}

$serverLog = Join-Path $logs "server.log"
$server = Start-Process $Godot -PassThru -WindowStyle Hidden `
    -ArgumentList "--headless", "--path", "`"$root`"", "--", "--server", "--port", $Port, "--dev-scenarios" `
    -RedirectStandardOutput $serverLog -RedirectStandardError (Join-Path $logs "server.err")

$ready = $false

for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 500

    if ((Test-Path $serverLog) -and (Select-String -Path $serverLog -Pattern "Server listening" -Quiet)) {
        $ready = $true
        break
    }
}

if (-not $ready) {
    Write-Host "The server did not start; see $serverLog"
    Stop-Process -Id $server.Id -ErrorAction SilentlyContinue
    exit 1
}

$failed = 0

foreach ($name in $Scenarios) {
    $log = Join-Path $logs "$name.log"
    $client = Start-Process $Godot -PassThru -WindowStyle Hidden `
        -ArgumentList "--headless", "--path", "`"$root`"", "--", "--port", $Port, "--profile", "fresh", "--name", "`"Test $name`"", "--scenario", $name `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $logs "$name.err")

    if (-not $client.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $client.Id -ErrorAction SilentlyContinue
        Write-Host "SCENARIO ${name}: FAIL did not finish within $TimeoutSeconds s (log: $log)"
        $failed++
        continue
    }

    $result = Select-String -Path $log -Pattern "SCENARIO ${name}: (PASS|FAIL)" | Select-Object -Last 1

    if ($null -eq $result) {
        Write-Host "SCENARIO ${name}: FAIL no result printed (log: $log)"
        $failed++
    }
    else {
        # A C# exception fails the scenario even when its steps passed: the game logs it
        # and plays on, so nothing else would notice.
        $exception = Select-String -Path $log -Pattern "Exception:" | Select-Object -First 1

        if ($result.Line -cmatch ": PASS" -and $null -ne $exception) {
            Write-Host ("SCENARIO ${name}: FAIL the client logged an exception: " + $exception.Line.Trim())
            $failed++
        }
        else {
            Write-Host $result.Line

            # Case-sensitive, on the result itself: a notice in a FAIL line can hold "Pass".
            if ($result.Line -cnotmatch ": PASS") {
                $failed++
            }
        }
    }
}

& (Join-Path $PSScriptRoot "server-stop.ps1") -Port $Port | Out-Null
Write-Host "$($Scenarios.Count - $failed) of $($Scenarios.Count) passed. Logs in $logs"
exit $failed
