param([ValidateSet('Mono','IL2CPP')][string]$Backend='Mono',[switch]$Tests,[ValidateSet('EditMode','PlayMode')][string]$TestMode='EditMode',[string]$Editor='D:/Unity/6000.6.0f1/Editor/Unity.exe',[string]$ProjectName='transport-example')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version=Split-Path (Split-Path (Split-Path $Editor -Parent) -Parent) -Leaf
if($version -match '^(\d+)\.(\d+)\.' -and ([int]$Matches[1] -lt 6000 -or ([int]$Matches[1] -eq 6000 -and [int]$Matches[2] -lt 5))){throw 'The Panel Renderer samples require Unity 6.5 or newer.'}
if($ProjectName -notmatch '^transport-[a-zA-Z0-9_-]+$'){throw 'Use a transport- project name.'}
$project=Join-Path $root "build/$ProjectName"
New-Item -ItemType Directory -Force -Path "$project/Assets/Editor","$project/ProjectSettings","$project/Packages" | Out-Null
Copy-Item -LiteralPath "$root/Validation/Transport/TransportPackageInstaller.cs" -Destination "$project/Assets/Editor" -Force
$version=Split-Path (Split-Path (Split-Path $Editor -Parent) -Parent) -Leaf
"m_EditorVersion: $version" | Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt"
$manifest=Join-Path $project 'Packages/manifest.json'
if(!(Test-Path -LiteralPath $manifest) -or (Get-Content -Raw -LiteralPath $manifest|ConvertFrom-Json).dependencies.'com.unity.transport' -ne '2.7.4'){
    $install=Start-Process -FilePath $editor -ArgumentList @('-batchmode','-nographics','-projectPath',$project,'-executeMethod','TransportPackageInstaller.Install','-logFile',"$project/install.log") -WindowStyle Hidden -Wait -PassThru
    if($install.ExitCode -ne 0){throw "Package installation failed: $project/install.log"}
}
foreach($package in @('com.nykwil.gekkonet','SharedGame')){Copy-Item -LiteralPath "$root/Unity/Packages/$package" -Destination "$project/Packages" -Recurse -Force}
New-Item -ItemType Directory -Force -Path "$project/Assets/Samples","$project/Assets/Validation" | Out-Null
foreach($sample in @('RollbackDuel','UnityTransport')){Copy-Item -LiteralPath "$root/Unity/Packages/SharedGame/Samples~/$sample" -Destination "$project/Assets/Samples" -Recurse -Force}
foreach($file in @('TransportValidationPlayer.cs','TransportValidation.asmdef','Editor','Tests')){
    Copy-Item -LiteralPath "$root/Validation/Transport/$file" -Destination "$project/Assets/Validation" -Recurse -Force
}
$arguments=@('-batchmode','-nographics','-projectPath',$project)
if($Tests){
    $arguments+=@('-runTests','-testPlatform',$TestMode,'-testResults',"$project/results-$TestMode.xml",'-logFile',"$project/tests-$TestMode.log")
}else{
    $arguments+=@('-quit','-executeMethod',"TransportValidationBuild.$Backend",'-logFile',"$project/build-$Backend.log")
}
$process=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw "Transport validation failed; inspect $project logs."}
if($Tests){
    [xml]$results=Get-Content -LiteralPath "$project/results-$TestMode.xml"
    if($results.'test-run'.failed -ne '0' -or [int]$results.'test-run'.passed -lt 1){throw 'Transport tests failed.'}
    Write-Output "PASS $($results.'test-run'.passed) transport tests"
}else{Write-Output "PASS Unity Transport $Backend build"}
