# Stops a running server so that it saves the players online before it quits. Ctrl+C
# or closing its window is a hard stop on Windows, which skips that save.
#
#   .\scripts\server-stop.ps1              the server on port 7070
#   .\scripts\server-stop.ps1 -Port 7071   another one
#
# It writes a stop file the server checks for (see game/server/StopSignals.cs), then
# waits for the server to quit.
param(
    [int]$Port = 7070,
    [int]$TimeoutSeconds = 20
)

# Godot's user data folder for this project, where user:// points on Windows.
$userData = Join-Path $env:APPDATA "Godot\app_userdata\mmo-game-3d"
$stopFile = Join-Path $userData "server-stop-$Port"

function Find-Servers {
    Get-CimInstance Win32_Process |
        Where-Object { $_.Name -like "Godot_*" -and $_.CommandLine -like "*--server*" } |
        ForEach-Object { $_.ProcessId }
}

if (@(Find-Servers).Count -eq 0) {
    Write-Host "No server is running."
    exit 0
}

New-Item -ItemType Directory -Force $userData | Out-Null
Set-Content -Path $stopFile -Value "stop"
Write-Host "Asked the server on port $Port to stop; waiting for it to save and quit..."

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    if (@(Find-Servers).Count -eq 0) {
        Write-Host "The server stopped."
        exit 0
    }

    Start-Sleep -Milliseconds 250
}

Write-Host "The server did not stop within $TimeoutSeconds seconds."
exit 1
