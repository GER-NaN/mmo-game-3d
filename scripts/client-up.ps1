# Builds the C# code, then starts the game in its own window. The console build of
# Godot opens a second window with the game's log.
#
#   .\scripts\client-up.ps1                    the "default" player, from the main menu
#   .\scripts\client-up.ps1 -Profile alice     another player on the same machine
#   .\scripts\client-up.ps1 -Profile fresh     a new player every launch
#   .\scripts\client-up.ps1 -AutoConnect       skip the main menu
param(
    [string]$Profile = "default",
    [switch]$AutoConnect,
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

# Godot runs the assembly it finds in .godot/mono, so an unbuilt change would run old code.
dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# The docs wiki, if a doc changed. A failure here never stops the game.
& (Join-Path $PSScriptRoot "wiki-publish.ps1") -IfChanged

# Arguments after "--" are the game's own; see game/LaunchOptions.cs.
$gameArgs = @("--path", "`"$root`"", "--", "--profile", $Profile)

if ($AutoConnect) {
    $gameArgs += "--autoconnect"
}

Start-Process $Godot -ArgumentList $gameArgs
