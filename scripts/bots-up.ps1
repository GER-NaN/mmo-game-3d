# Starts bot clients: each is a whole client in its own window that plays by itself
# (--bot), with no sound. See docs/engineering/bot-testing.md. Needs a server
# (scripts/server-up.ps1).
#
#   .\scripts\bots-up.ps1                  10 bots, soak1 to soak10, one of each persona,
#                                          tiled over every screen
#   .\scripts\bots-up.ps1 -Count 4         4 bots
#   .\scripts\bots-up.ps1 -Layout Full     each window the size of its screen, stacked:
#                                          bigger game views and pictures, for runs
#                                          nobody watches
#   .\scripts\bots-up.ps1 -Personas gamer,gamer,curious
#                                          who each bot is, in turn (game/dev/BotPersonas.cs);
#                                          the default mix has one of each
#
# Each bot is a player of its own (profile soakN), kept between runs, and logs to
# %TEMP%\mmo-game-3d-bots\soakN.log (replaced each run). Stop them with
# .\scripts\bots-stop.ps1; stop the server with server-stop.ps1 so it saves.
param(
    [int]$Count = 10,
    [ValidateSet("Tiled", "Full")]
    [string]$Layout = "Tiled",
    [string[]]$Personas = @("dropper", "curious", "gamer", "escaper", "wedger", "earner", "slow", "masher", "shadow", "eventer"),
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "mmo-game-3d.sln") | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit $LASTEXITCODE
}

# A log each: the default log is one file every client on the machine shares.
$logs = Join-Path ([System.IO.Path]::GetTempPath()) "mmo-game-3d-bots"
New-Item -ItemType Directory -Force $logs | Out-Null

Add-Type -AssemblyName System.Windows.Forms
# Shared evenly over every screen, in a grid on each: 8 bots on two screens is 2 by 2 on
# each. Full: each window the whole screen, the screen's bots on top of each other.
$screens = @([System.Windows.Forms.Screen]::AllScreens | Sort-Object { $_.WorkingArea.X })
$perScreen = [Math]::Ceiling($Count / $screens.Count)
$columns = [Math]::Ceiling([Math]::Sqrt($perScreen))
$rows = [Math]::Ceiling($perScreen / $columns)

if ($Layout -eq "Full") {
    $columns = 1
    $rows = 1
}

# Godot counts --position from the desktop's top-left corner, not the main screen's: a
# screen left of the main one has negative coordinates in Windows.
$desktop = [System.Windows.Forms.SystemInformation]::VirtualScreen

for ($i = 0; $i -lt $Count; $i++) {
    $area = $screens[[Math]::Floor($i / $perScreen)].WorkingArea
    $slot = if ($Layout -eq "Full") { 0 } else { $i % $perScreen }
    # Room for the title bar and frame round each window.
    $width = [Math]::Floor($area.Width / $columns) - 16
    $height = [Math]::Floor($area.Height / $rows) - 40
    $x = $area.X - $desktop.X + ($slot % $columns) * ($width + 16) + 8
    $y = $area.Y - $desktop.Y + [Math]::Floor($slot / $columns) * ($height + 40) + 31
    $n = $i + 1
    $persona = $Personas[$i % $Personas.Count]
    # Before "--": the engine's own (window, no sound). After: the game's.
    $gameArgs = @(
        "--path", "`"$root`"", "--audio-driver", "Dummy",
        "--log-file", "`"$(Join-Path $logs "soak$n.log")`"",
        "--resolution", "${width}x${height}", "--position", "$x,$y",
        "--", "--profile", "soak$n", "--name", "Soak$n", "--bot", "--persona", $persona, "--windowed"
    )
    Start-Process $Godot -ArgumentList $gameArgs -WindowStyle Minimized
}

Write-Host "$Count bots started ($Layout). Stop them with .\scripts\bots-stop.ps1"
