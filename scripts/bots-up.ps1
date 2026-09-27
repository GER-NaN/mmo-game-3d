# Starts bot clients you can watch: each is a whole client in its own window that plays
# by itself (--bot), tiled over every screen, with no sound. For soak runs and for
# watching the game with people in it. Needs a server (scripts/server-up.ps1).
#
#   .\scripts\bots-up.ps1              8 bots, soak1 to soak8
#   .\scripts\bots-up.ps1 -Count 4     4 bots
#
# Each bot is a player of its own (profile soakN), kept between runs. Stop them with
# .\scripts\bots-stop.ps1; stop the server with server-stop.ps1 so it saves.
param(
    [int]$Count = 8,
    [string]$Godot = "C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)

$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "mmo-game-3d.sln") | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit $LASTEXITCODE
}

Add-Type -AssemblyName System.Windows.Forms
# Shared evenly over every screen, in a grid on each: 8 bots on two screens is 2 by 2 on
# each.
$screens = @([System.Windows.Forms.Screen]::AllScreens | Sort-Object { $_.WorkingArea.X })
$perScreen = [Math]::Ceiling($Count / $screens.Count)
$columns = [Math]::Ceiling([Math]::Sqrt($perScreen))
$rows = [Math]::Ceiling($perScreen / $columns)

for ($i = 0; $i -lt $Count; $i++) {
    $area = $screens[[Math]::Floor($i / $perScreen)].WorkingArea
    $slot = $i % $perScreen
    # Room for the title bar and frame round each window.
    $width = [Math]::Floor($area.Width / $columns) - 16
    $height = [Math]::Floor($area.Height / $rows) - 40
    $x = $area.X + ($slot % $columns) * ($width + 16) + 8
    $y = $area.Y + [Math]::Floor($slot / $columns) * ($height + 40) + 31
    $n = $i + 1
    # Before "--": the engine's own (window, no sound). After: the game's.
    $gameArgs = @(
        "--path", "`"$root`"", "--audio-driver", "Dummy",
        "--resolution", "${width}x${height}", "--position", "$x,$y",
        "--", "--profile", "soak$n", "--name", "Soak$n", "--bot", "--windowed"
    )
    Start-Process $Godot -ArgumentList $gameArgs -WindowStyle Minimized
}

Write-Host "$Count bots started. Stop them with .\scripts\bots-stop.ps1"
