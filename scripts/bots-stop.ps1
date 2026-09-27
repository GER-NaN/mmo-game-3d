# Closes the bot clients that scripts/bots-up.ps1 started. The server saves each bot
# as it disconnects. Closing a C# Godot process from outside prints "Fatal error.
# Internal CLR error" in its console: that is the close, not a bug.
$bots = Get-CimInstance Win32_Process |
    Where-Object { $_.Name -like "Godot_*" -and $_.CommandLine -like "*--bot*" -and $_.CommandLine -like "*--profile soak*" }

foreach ($bot in $bots) {
    Stop-Process -Id $bot.ProcessId -Force
}

# Each bot is two processes, the console and the game.
$profiles = @($bots | ForEach-Object { if ($_.CommandLine -match '--profile (soak\d+)') { $matches[1] } } | Sort-Object -Unique)
Write-Host ($profiles.Count.ToString() + " bots stopped.")
