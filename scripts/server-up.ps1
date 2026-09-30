# Builds the C# code, then starts the game as a headless server in its own window,
# which shows the server's log. It needs Postgres running with the mmo3d database.
#
# Stop it with .\scripts\server-stop.ps1, which lets it save the players online.
# Ctrl+C or closing the window is a hard stop that skips that save.
param(
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

# A second server cannot listen on the same port, and its window closes before the
# reason can be read.
$running = Get-NetUDPEndpoint -LocalPort 7070 -ErrorAction SilentlyContinue | Select-Object -First 1

if ($running) {
    Write-Host "A server is already running on port 7070 (process $($running.OwningProcess)). Use it, or stop it first with .\scripts\server-stop.ps1."
    exit 1
}

dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Arguments after "--" are the game's own; see game/LaunchOptions.cs.
Start-Process $Godot -ArgumentList "--headless", "--path", "`"$root`"", "--", "--server"
