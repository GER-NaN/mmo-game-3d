# Builds the C# code, then runs the bots through the Overseer (tools/overseer). Each
# bot plays in its own game client and reports to its folder under bot-runs/.
#
#   .\scripts\bots-run.ps1                                one bot, to the main menu (game/bots/BotMain.tscn)
#   .\scripts\bots-run.ps1 -Bot QuitBotMain               clicks Quit on the main menu
#   .\scripts\bots-run.ps1 -Bot FullscreenBotMain         turns fullscreen on and off in the settings
#   .\scripts\bots-run.ps1 -Bot JumpBotMain -Connect      into the world, jumps once (starts the server if needed)
#   .\scripts\bots-run.ps1 -Bot MeadowsBotMain -Connect   walks through the doors to the meadows
#   .\scripts\bots-run.ps1 -Bot EquipPhoneBotMain -Fresh  a new player equips its phone
#   .\scripts\bots-run.ps1 -Timeout 60                    give it longer
param(
    [string]$Bot = "BotMain",
    [switch]$Connect,
    [switch]$Fresh,
    [double]$Timeout = 30,
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
)

$root = Split-Path $PSScriptRoot -Parent

# Godot runs the assembly it finds in .godot/mono, so an unbuilt change would run old code.
dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$overseerArgs = @("--godot", $Godot, "--project", $root, "--bot", $Bot, "--timeout", $Timeout)

if ($Connect) {
    $overseerArgs += "--connect"
}

if ($Fresh) {
    $overseerArgs += "--fresh"
}

dotnet run --no-build --project (Join-Path $root "tools\overseer\Overseer.csproj") -- @overseerArgs
exit $LASTEXITCODE
