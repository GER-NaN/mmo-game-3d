# Starts one bot that does every activity there is, in a shuffled order, each to its end
# (nothing cancels or breaks into it), and prints a tally after each round: what
# finished, what it gave up on and why, and what could not start. A goal meets an
# activity's needs first (a phone, money, a career). Needs a server
# (scripts/server-up.ps1). See docs/engineering/bot-testing.md.
#
#   .\scripts\bot-everything.ps1
#   .\scripts\bot-everything.ps1 -Persona slow
#
# The bot is the player "everybot" (profile everybot), in a window the size of the
# screen, and logs to %TEMP%\mmo-game-3d-bots\everybot.log; the tallies are the lines
# "Bot: everything, round N over". Close its window to stop it.
param(
    [string]$Persona = "wanderer",
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "mmo-game-3d.sln") | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit $LASTEXITCODE
}

$logs = Join-Path ([System.IO.Path]::GetTempPath()) "mmo-game-3d-bots"
New-Item -ItemType Directory -Force $logs | Out-Null
Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$gameArgs = @(
    "--path", "`"$root`"", "--audio-driver", "Dummy",
    "--log-file", "`"$(Join-Path $logs "everybot.log")`"",
    "--resolution", "$($screen.Width)x$($screen.Height)",
    "--", "--profile", "everybot", "--name", "Everybot", "--bot-everything", "--persona", $Persona, "--windowed"
)
Start-Process $Godot -ArgumentList $gameArgs -WindowStyle Minimized
Write-Host "A bot started that does everything. It logs to $(Join-Path $logs "everybot.log")."
