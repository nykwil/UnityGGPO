param([int]$Players = 2, [switch]$Spectator, [switch]$LateJoin, [switch]$Impaired, [int]$Frames = 600, [int]$BasePort = 29500, [int]$LatencyMs = 50, [int]$JitterMs = 15, [double]$Loss = .02, [int]$LeavePlayer = -1, [string]$NativeLibrary)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ($Players -lt 2 -or $Players -gt 4) { throw 'Use 2–4 players.' }
if ($LeavePlayer -ge $Players -or $LeavePlayer -lt -1) { throw 'Invalid leaving player index.' }
& dotnet build "$root/Validation/SharedGame.Validation.csproj" -c Release --nologo
if ($LASTEXITCODE) { throw 'Harness build failed.' }
$directory = Join-Path $root "build/network-$Players-$BasePort"
New-Item -ItemType Directory -Force -Path $directory | Out-Null
if ($NativeLibrary) {
    Copy-Item -LiteralPath $NativeLibrary -Destination "$root/Validation/bin/Release/net10.0/GekkoNet.dll" -Force
}
Get-FileHash -LiteralPath "$root/Validation/bin/Release/net10.0/GekkoNet.dll" | Select-Object Hash | ConvertTo-Json | Set-Content -LiteralPath "$directory/native-binary.json"
$jobs = @()
$proxy = $null
try {
    if ($Impaired) {
        if ($Players -ne 2 -or $Spectator) { throw 'The pair proxy test uses two players without a spectator.' }
        $proxyPort = $BasePort + 20
        $rightPort = $BasePort + 1
        $proxy = Start-Process -FilePath python -ArgumentList @("$root/scripts/udp-test-proxy.py",'--listen',"127.0.0.1:$proxyPort",'--left',"127.0.0.1:$BasePort",'--right',"127.0.0.1:$rightPort",'--latency-ms',$LatencyMs,'--jitter-ms',$JitterMs,'--loss',$Loss,'--seed',7) -WindowStyle Hidden -PassThru -RedirectStandardOutput "$directory/proxy.txt" -RedirectStandardError "$directory/proxy.err"
        Start-Sleep -Milliseconds 300
    }
    for ($i = 0; $i -lt $Players; $i++) {
        $mode = if ($i -eq $LeavePlayer) { 'peer-drop' } elseif ($LeavePlayer -ge 0) { 'peer-trace' } else { 'peer' }
        $arguments = @("$root/Validation/bin/Release/net10.0/SharedGame.Validation.dll",$mode,$Players,$i,$Frames,$BasePort)
        if ($Impaired) { $arguments += $proxyPort }
        $jobs += Start-Process -FilePath dotnet -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput "$directory/player-$i.txt" -RedirectStandardError "$directory/player-$i.err"
    }
    if ($Spectator) {
        if ($LateJoin) { Start-Sleep -Seconds 4 }
        $jobs += Start-Process -FilePath dotnet -ArgumentList @("$root/Validation/bin/Release/net10.0/SharedGame.Validation.dll",'peer',$Players,-1,$Frames,$BasePort) -WindowStyle Hidden -PassThru -RedirectStandardOutput "$directory/spectator.txt" -RedirectStandardError "$directory/spectator.err"
    }
    foreach ($job in $jobs) {
        if (!$job.WaitForExit(60000)) { throw 'Network test process timed out.' }
        $job.Refresh()
        if ($job.ExitCode -ne 0) { throw "Network process failed: inspect $directory" }
    }
    $files = @(for ($i = 0; $i -lt $Players; $i++) { if ($i -ne $LeavePlayer) { "$directory/player-$i.txt" } })
    if ($Spectator) { $files += "$directory/spectator.txt" }
    $checksums = @()
    foreach ($file in $files) {
        $line = (Get-Content -LiteralPath $file | Where-Object { $_ -like 'RESULT *' }) -join ''
        Write-Output $line
        if ($line -notmatch "RESULT frame=$Frames checksum=([0-9A-F]+) desyncs=0") { throw "Missing successful verdict: $file" }
        $checksums += $Matches[1]
    }
    if (@($checksums | Select-Object -Unique).Count -ne 1) { throw 'Confirmed historical checksums differ.' }
    Write-Output "PASS $Players peers, spectator=$Spectator lateJoin=${LateJoin}: checksum agreement at frame $Frames"
} finally {
    foreach ($job in $jobs) { if (!$job.HasExited) { Stop-Process -Id $job.Id } }
    if ($proxy -and !$proxy.HasExited) { Stop-Process -Id $proxy.Id }
}
