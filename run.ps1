Start-Process -FilePath "D:/Games/Clone Drone In The Danger Zone/Clone Drone in the Danger Zone.exe"
Start-Sleep -Seconds 1

$process = Get-Process -Name "Clone Drone in the Danger Zone"
$_ = (New-Object -ComObject WScript.Shell).AppActivate($process.MainWindowTitle)

while ($true)
{
    if ($process.HasExited) {
        break;
    }
    Start-Sleep -Seconds 1
}
