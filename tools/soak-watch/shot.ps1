# Saves a picture of one bot's game window: .\shot.ps1 soak3 C:\path\to\file.png
# It only reads the screen; it sends the game no input.
param(
    [string]$Profile,
    [string]$Path
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public struct ShotRect { public int L, T, R, B; }
public static class ShotWin { [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out ShotRect r); }
"@

$game = Get-CimInstance Win32_Process |
    Where-Object { $_.Name -eq "Godot_v4.7.2-stable_mono_win64.exe" -and $_.CommandLine -like "*--profile $Profile *" } |
    Select-Object -First 1

if (-not $game) {
    exit 1
}

$window = (Get-Process -Id $game.ProcessId).MainWindowHandle
$r = New-Object ShotRect
[void][ShotWin]::GetWindowRect($window, [ref]$r)
$bitmap = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($r.L, $r.T, 0, 0, $bitmap.Size)
$bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
