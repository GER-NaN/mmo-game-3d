# Records the memory of the server and each bot every few minutes, to see a leak over a
# long bot run. Stops when the server does. See README.md.
#
#   powershell -File tools\bot-watch\memory.ps1                 every 5 minutes
#   powershell -File tools\bot-watch\memory.ps1 -EverySeconds 60
#
# Writes %TEMP%\mmo-game-3d-bots\memory.csv: time, process id, who (server or the bot's
# profile), working set and private memory in MB.
param(
    [int]$EverySeconds = 300
)

$logs = Join-Path ([System.IO.Path]::GetTempPath()) "mmo-game-3d-bots"
New-Item -ItemType Directory -Force $logs | Out-Null
$csv = Join-Path $logs "memory.csv"

if (-not (Test-Path $csv)) {
    "time,process,profile,working_set_mb,private_mb" | Set-Content -Encoding utf8 $csv
}

$first = $null

while ($true) {
    # The console exe only starts the game exe, which holds the memory.
    $all = Get-CimInstance Win32_Process | Where-Object { $_.Name -like "Godot*" -and $_.Name -notlike "*_console.exe" }
    # The bots' server; the dev scenarios' own (port 7071) comes and goes.
    $server = @($all | Where-Object { $_.CommandLine -like "*--server*" -and $_.CommandLine -notlike "*--port 7071*" } | ForEach-Object { $_.ProcessId })

    # The server that was up at the start: a restarted one gets a recorder of its own,
    # or two would write every row twice.
    if ($null -eq $first) {
        $first = $server
    }

    if (@($server | Where-Object { $first -contains $_ }).Count -eq 0) {
        Write-Host "The server stopped; stopped."
        break
    }

    $time = Get-Date -Format "yyyy-MM-dd HH:mm"

    foreach ($process in $all) {
        if ($process.CommandLine -like "*--server*") {
            $who = "server"
        }
        elseif ($process.CommandLine -match '--profile (\S+)') {
            $who = $matches[1]
        }
        else {
            continue
        }

        $running = Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue

        if ($running) {
            "{0},{1},{2},{3},{4}" -f $time, $process.ProcessId, $who, [int]($running.WorkingSet64 / 1MB), [int]($running.PrivateMemorySize64 / 1MB) | Add-Content -Encoding utf8 $csv
        }
    }

    Start-Sleep -Seconds $EverySeconds
}
