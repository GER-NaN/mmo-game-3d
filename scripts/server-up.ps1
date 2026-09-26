# Builds the C# code, then starts the game as a headless server in its own window,
# which shows the server's log.
param(
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Arguments after "--" are the game's own; Main reads --server from them.
Start-Process $Godot -ArgumentList "--headless", "--path", "`"$root`"", "--", "--server"
