# Starts one bot that does only one activity or goal, again and again, in a window you
# can watch: to try a new one, or to replay what a finding caught. Needs a server
# (scripts/server-up.ps1). See docs/engineering/bot-testing.md.
#
#   .\scripts\bot-try.ps1 "edit my Whois page"
#   .\scripts\bot-try.ps1 "fight drones" -Persona slow
#
# The bot is the player "trybot" (profile trybot), and logs to
# %TEMP%\mmo-game-3d-bots\trybot.log. Close its window to stop it.
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Only,
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
$gameArgs = @(
    "--path", "`"$root`"", "--audio-driver", "Dummy",
    "--log-file", "`"$(Join-Path $logs "trybot.log")`"",
    "--resolution", "1280x720",
    "--", "--profile", "trybot", "--name", "Trybot", "--bot", "--persona", $Persona, "--bot-only", "`"$Only`"", "--windowed"
)
Start-Process $Godot -ArgumentList $gameArgs -WindowStyle Minimized
Write-Host "A bot started that only does `"$Only`". It logs to $(Join-Path $logs "trybot.log")."
