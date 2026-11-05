param(
    [string]$Editor = 'D:/Unity/6000.6.0f1/Editor/Unity.exe',
    [string]$Name = 'unity66',
    [switch]$CoreOnly,
    [switch]$PlayMode,
    [switch]$NoDomainReload
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = Split-Path (Split-Path (Split-Path $Editor -Parent) -Parent) -Leaf
if($version -match '^(\d+)\.(\d+)\.' -and ([int]$Matches[1] -lt 6000 -or ([int]$Matches[1] -eq 6000 -and [int]$Matches[2] -lt 5))){throw 'Panel Renderer requires Unity 6.5 or newer.'}
if ($Name -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple validation name.' }
$project = Join-Path $root "build/validation-$Name"
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    $target = [IO.Path]::GetFullPath((Join-Path $project $folder))
    if (!$target.StartsWith([IO.Path]::GetFullPath($project) + [IO.Path]::DirectorySeparatorChar)) { throw 'Unsafe validation path.' }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
}
New-Item -ItemType Directory -Force -Path "$project/Assets","$project/Packages","$project/ProjectSettings" | Out-Null
$version = Split-Path (Split-Path (Split-Path $Editor -Parent) -Parent) -Leaf
"m_EditorVersion: $version" | Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt"
# -CoreOnly installs just the GekkoNet package (bindings plus session core), as a game without SharedGame would.
$packageNames = if ($CoreOnly) { @('com.nykwil.gekkonet') } else { @('com.nykwil.gekkonet','SharedGame') }
foreach ($package in $packageNames) {
    $destination = Join-Path $project "Packages/$package"
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -Path "$root/Unity/Packages/$package/*" -Destination $destination -Recurse -Force
}
New-Item -ItemType Directory -Force -Path "$project/Assets/Tests","$project/Assets/Editor" | Out-Null
Copy-Item -LiteralPath "$root/Validation/ConfigureTests.cs" -Destination "$project/Assets/Editor" -Force
if (!$CoreOnly) {
    New-Item -ItemType Directory -Force -Path "$project/Assets/VectorWar" | Out-Null
    Copy-Item -LiteralPath "$root/Unity/Assets/VectorWar/Runtime" -Destination "$project/Assets/VectorWar" -Recurse -Force
    Copy-Item -LiteralPath "$root/Unity/Assets/Tests/EditMode" -Destination "$project/Assets/Tests" -Recurse -Force
} else {
    Copy-Item -LiteralPath "$root/Validation/CorePackageTests.cs","$root/Validation/CorePackage.Tests.asmdef" -Destination "$project/Assets/Tests" -Force
}
if ($PlayMode) {
    Copy-Item -LiteralPath "$root/Unity/Assets/Tests/PlayMode" -Destination "$project/Assets/Tests" -Recurse -Force
}
if (!$CoreOnly) {
    Copy-Item -LiteralPath "$root/Unity/Packages/SharedGame/Samples~/RollbackDuel" -Destination "$project/Assets" -Recurse -Force
    Copy-Item -LiteralPath "$root/Unity/Assets/Editor" -Destination "$project/Assets" -Recurse -Force
}
@'
{"dependencies":{"com.unity.inputsystem":"1.20.1","com.unity.test-framework":"1.8.0","com.unity.modules.uielements":"1.0.0","com.unity.modules.physics":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}
'@ | Set-Content -LiteralPath "$project/Packages/manifest.json"
if ($CoreOnly) {
    '{"dependencies":{"com.unity.test-framework":"1.8.0"}}' | Set-Content -LiteralPath "$project/Packages/manifest.json"
}
# The example assets can reference art outside this isolated compiler/test project.
$mode = if ($PlayMode) { 'PlayMode' } else { 'EditMode' }
$arguments = @('-batchmode','-nographics','-projectPath', $project, '-runTests', '-testPlatform', $mode, '-testResults', "$project/results.xml", '-logFile', "$project/editor.log")
$previousReload = $env:GEKKO_TEST_DISABLE_RELOAD
try {
    $env:GEKKO_TEST_DISABLE_RELOAD = if ($NoDomainReload) { '1' } else { '0' }
    $process = Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
} finally { $env:GEKKO_TEST_DISABLE_RELOAD = $previousReload }
if ($process.ExitCode) { throw "Unity $mode validation exited $($process.ExitCode). See $project/editor.log" }
if (!(Test-Path -LiteralPath "$project/results.xml")) { throw "Unity did not produce a test verdict. See $project/editor.log" }
[xml]$report = Get-Content -Raw -LiteralPath "$project/results.xml"
if ($report.'test-run'.failed -ne '0') { throw 'Unity tests failed.' }
Write-Output "Unity $mode passed: $project/results.xml"
