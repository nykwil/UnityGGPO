param([int]$Players=2,[switch]$Spectator,[switch]$LateJoin,[switch]$CrossBackend,[switch]$Impaired,[switch]$Stress,[int]$RestartCount=0,[int]$BasePort=32000,[string]$Mono='build/players/Mono/VectorWar.exe',[string]$IL2CPP='build/players/IL2CPP/VectorWar.exe')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$directory=Join-Path $root "build/player-network-$BasePort"
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$jobs=@()
$results=@()
$proxy=$null
if ($Players -lt 2 -or $Players -gt 4) { throw 'Use 2–4 players.' }
if ($Impaired -and ($Players -ne 2 -or $Spectator)) { throw 'The pair proxy requires two players without a spectator.' }
if ($RestartCount -lt 0 -or $RestartCount -gt 50 -or ($RestartCount -gt 0 -and ($RestartCount -lt 3 -or !$Stress))) { throw 'Use stress mode for resource validation (3–50 restarts, allowing warm-up).' }
if ($Stress -and ($Spectator -or $LateJoin -or $Impaired -or $CrossBackend)) { throw 'Stress validation runs one process without network flags.' }
try {
    if ($Impaired) {
        $proxyPort=$BasePort+20
        $proxy=Start-Process -FilePath python -ArgumentList @("$root/scripts/udp-test-proxy.py",'--listen',"127.0.0.1:$proxyPort",'--left',"127.0.0.1:$BasePort",'--right',"127.0.0.1:$($BasePort+1)",'--latency-ms',50,'--jitter-ms',15,'--loss',.02,'--seed',7) -WindowStyle Hidden -PassThru -RedirectStandardOutput "$directory/proxy.txt" -RedirectStandardError "$directory/proxy.err"
        Start-Sleep -Milliseconds 300
    }
    $indices=@(0..($Players-1))
    if ($Stress) { $indices=@(0) }
    if ($Spectator) { $indices+=-1 }
    foreach ($index in $indices) {
        if ($index -eq -1 -and $LateJoin) { Start-Sleep -Seconds 4 }
        $name=if($index -lt 0){'spectator'}else{"player-$index"}
        $output=Join-Path $directory "$name.json"
        $configuration=Join-Path $directory "$name-config.json"
        @{Players=$Players;LocalPlayer=[Math]::Max(0,$index);BasePort=$BasePort;TargetFrame=600;Spectator=($index -lt 0);Stress=[bool]$Stress;RestartCount=$RestartCount;ProxyPort=$(if($Impaired){$proxyPort}else{0});LeavePlayer=-1;Output=$output} | ConvertTo-Json | Set-Content -LiteralPath $configuration
        $executable=if($CrossBackend -and $index -eq 1){$IL2CPP}else{$Mono}
        $executable=[IO.Path]::GetFullPath((Join-Path $root $executable))
        if (!(Test-Path -LiteralPath $executable)){throw "Missing player: $executable"}
        $arguments=@('-batchmode','-nographics','--session-validation',('"'+$configuration+'"'),'-logFile',('"'+(Join-Path $directory "$name.log")+'"'))
        $jobs+=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $results+=$output
    }
    foreach($job in $jobs){
        if(!$job.WaitForExit([Math]::Max(120000,($RestartCount+1)*5000))){throw 'Unity player validation timed out.'}
        $job.Refresh()
        if($job.ExitCode -ne 0){throw "Player failed; inspect $directory"}
    }
    $reports=@($results | ForEach-Object { Get-Content -Raw -LiteralPath $_ | ConvertFrom-Json })
    if ($RestartCount -gt 0) {
        $report=$reports[0]
        if($report.Error -or $report.RestartMemory.Count -ne $RestartCount+1){throw 'Incomplete resource validation.'}
        $warm=@($report.RestartMemory | Select-Object -Skip 2)
        $range=($warm | Measure-Object -Maximum -Minimum)
        $growth=$range.Maximum-$range.Minimum
        Write-Output "Resource restarts=$RestartCount retained allocation range=$growth bytes; samples: $($report.RestartMemory -join ',')"
        if($growth -gt 8MB){throw 'Retained engine memory varied by more than 8 MB after warm-up.'}
        return
    }
    $common=@{}
    foreach($entry in $reports[0].Checksums){$common[[int]$entry.Frame]=[long]$entry.Checksum}
    foreach($report in $reports){
        if($report.Error -or $report.Desyncs -ne 0){throw "Player error: $($report.Error)"}
        $map=@{}
        foreach($entry in $report.Checksums){$map[[int]$entry.Frame]=[long]$entry.Checksum}
        foreach($frame in @($common.Keys)){
            if(!$map.ContainsKey($frame)){$common.Remove($frame)}
            elseif($map[$frame] -ne $common[$frame]){throw "Checksum mismatch at frame $frame"}
        }
        Write-Output "$($report.UnityVersion) $($report.Backend): frame $($report.FinalFrame), rollbacks $($report.Rollbacks), stalls $($report.Stalls)"
    }
    if($common.Count -lt 5){throw 'Insufficient matching historical frames.'}
    $frame=@($common.Keys | Sort-Object)[0]
    Write-Output "PASS Unity players=$Players spectator=$Spectator crossBackend=${CrossBackend}: $($common.Count) matching frames; frame $frame checksum $($common[$frame].ToString('X8'))"
}finally{
    foreach($job in $jobs){if(!$job.HasExited){Stop-Process -Id $job.Id}}
    if($proxy -and !$proxy.HasExited){Stop-Process -Id $proxy.Id}
}
