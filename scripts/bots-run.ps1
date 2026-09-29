# Builds the C# code, then runs bots through the Overseer (game/bots/overseer). Each bot
# plays one activity from game/bots/BotActivities.cs in its own game client, all at
# once, and reports to its own folder under bot-runs/<run>/.
#
#   .\scripts\bots-run.ps1                                   one bot, to the main menu
#   .\scripts\bots-run.ps1 -Bots quit                        clicks Quit on the main menu
#   .\scripts\bots-run.ps1 -Bots jump:connect                into the world as its own kept player
#   .\scripts\bots-run.ps1 -Bots equip-phone:fresh           as a new player
#   .\scripts\bots-run.ps1 -Bots quit,jump:connect           several at once
#   .\scripts\bots-run.ps1 -All                              every activity below, at once
#
# A bot is "activity", "activity:connect" (a kept player of its own, "bot-<activity>")
# or "activity:fresh" (a new player). Connected bots start the server if none runs.
param(
    [string[]]$Bots = @("main-menu"),
    [switch]$All,
    [double]$Timeout = 120,
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
)

# Every activity, each with the player it needs.
$allBots = @(
    "main-menu",
    "quit",
    "fullscreen",
    "jump:connect",
    "meadows:connect",
    "old-town-explorer:connect",
    "equip-phone:fresh",
    "phone-terminal:fresh",
    "surveyor-town:fresh",
    "surveyor-new_town:fresh",
    "surveillance-town:fresh"
)

$root = Split-Path $PSScriptRoot -Parent

# Godot runs the assembly it finds in .godot/mono, so an unbuilt change would run old code.
dotnet build (Join-Path $root "mmo-game-3d.sln")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($All) {
    $Bots = $allBots
}

$overseerArgs = @("--godot", $Godot, "--project", $root, "--timeout", $Timeout)

foreach ($bot in $Bots) {
    $overseerArgs += @("--bot", $bot)
}

dotnet run --no-build --project (Join-Path $root "game\bots\overseer\Overseer.csproj") -- @overseerArgs
exit $LASTEXITCODE
