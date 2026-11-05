param([ValidateRange(2,4)][int]$Players=2,[switch]$Spectator,[switch]$LateJoin,[switch]$CrossBackend,[switch]$Impaired,[int]$BasePort=35000)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$directory=Join-Path $root "build/transport-network-$BasePort"
New-Item -ItemType Directory -Force -Path $directory | Out-Null
if($Impaired -and ($Players -ne 2 -or $Spectator)){throw 'The UDP proxy uses two peers without a spectator.'}
$jobs=@();$outputs=@();$proxy=$null
try {
    if($Impaired){
        $proxyPort=$BasePort+20
        $proxy=Start-Process -FilePath python -ArgumentList @("$root/scripts/udp-test-proxy.py",'--listen',"127.0.0.1:$proxyPort",'--left',"127.0.0.1:$BasePort",'--right',"127.0.0.1:$($BasePort+1)",'--latency-ms',50,'--jitter-ms',15,'--loss',.02,'--seed',7) -WindowStyle Hidden -PassThru -RedirectStandardOutput "$directory/proxy.txt" -RedirectStandardError "$directory/proxy.err"
        Start-Sleep -Milliseconds 300
    }
    $indices=@(0..($Players-1));if($Spectator){$indices+=-1}
    foreach($index in $indices){
        if($index -eq -1 -and $LateJoin){Start-Sleep -Seconds 4}
        $name=if($index -lt 0){'spectator'}else{"player-$index"}
        $output=Join-Path $directory "$name.json";$config=Join-Path $directory "$name-config.json"
        @{Players=$Players;LocalPlayer=[Math]::Max(0,$index);BasePort=$BasePort;Spectator=($index -lt 0);ProxyPort=$(if($Impaired){$proxyPort}else{0});Output=$output}|ConvertTo-Json|Set-Content -LiteralPath $config
        $backend=if($CrossBackend -and $index -eq 1){'IL2CPP'}else{'Mono'}
        $exe=Join-Path $root "build/players/UnityTransport-$backend/TransportDuel.exe"
        if(!(Test-Path -LiteralPath $exe)){throw "Build the $backend Transport sample first."}
        $jobs+=Start-Process -FilePath $exe -ArgumentList @('-batchmode','-nographics','--transport-validation',('"'+$config+'"'),'-logFile',('"'+(Join-Path $directory "$name.log")+'"')) -WindowStyle Hidden -PassThru
        $outputs+=$output
    }
    foreach($job in $jobs){
        if(!$job.WaitForExit(90000)){throw "Transport validation timed out: $directory"}
        $job.Refresh();if($job.ExitCode -ne 0){throw "Transport player failed: $directory"}
    }
    $common=@{};$first=$true
    foreach($output in $outputs){
        $report=Get-Content -Raw -LiteralPath $output|ConvertFrom-Json
        if($report.Error -or $report.Desyncs -ne 0){throw "Transport error: $($report.Error)"}
        $frames=@{};foreach($entry in $report.Checksums){$frames[[int]$entry.Frame]=[long]$entry.Checksum}
        if($first){foreach($frame in $frames.Keys){$common[$frame]=$frames[$frame]};$first=$false}
        else{foreach($frame in @($common.Keys)){if(!$frames.ContainsKey($frame)){$common.Remove($frame)}elseif($frames[$frame] -ne $common[$frame]){throw "Transport checksum mismatch at $frame"}}}
        Write-Output "$($report.Backend): frame $($report.FinalFrame), desyncs=$($report.Desyncs)"
    }
    if($common.Count -lt 10){throw 'Insufficient common historical frames.'}
    $frame=@($common.Keys|Sort-Object)[0]
    Write-Output "PASS Unity Transport: players=$Players spectator=$Spectator crossBackend=$CrossBackend impaired=$Impaired; $($common.Count) matching frames; frame $frame checksum $($common[$frame].ToString('X8'))"
} finally {
    foreach($job in $jobs){if(!$job.HasExited){Stop-Process -Id $job.Id}}
    if($proxy -and !$proxy.HasExited){Stop-Process -Id $proxy.Id}
}
