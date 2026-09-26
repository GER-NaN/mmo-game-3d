# Builds the C# code, then starts the game in its own window. The console build of
# Godot opens a second window with the game's log.
param(
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

# Godot runs the assembly it finds in .godot/mono, so an unbuilt change would run old code.
dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Start-Process $Godot -ArgumentList "--path", "`"$root`""
