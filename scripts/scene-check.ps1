# Reports what an editor save would lose from each scene under game/, and exits 1 if
# any scene would lose something. A scene written by hand can work in the game and still
# lose a change on its first save in the editor: a change to an instanced scene's child
# is kept only when that instance has editable children ([editable path="..."]).
#
#   .\scripts\scene-check.ps1
#
# The check is game/dev/SceneCheck.cs, run with --check-scenes.
param(
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "mmo-game-3d.sln") | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit 1
}

& $Godot --headless --path "$root" -- --check-scenes 2>$null | Where-Object { $_ -like "Scene check:*" }
exit $LASTEXITCODE
