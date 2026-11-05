# Release-hygiene checks for the distributable packages. Runs locally and in CI.
#  - every package file and folder (outside Samples~) has a committed .meta
#  - the shipped GekkoNet.dll, patch and pinned header match build-provenance.json
#  - the GekkoNet submodule pin matches the provenance revision
#  - SharedGame's GekkoNet dependency matches the GekkoNet package version
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$packages = Join-Path $root 'Unity/Packages'
$failures = [Collections.Generic.List[string]]::new()

foreach ($package in 'com.nykwil.gekkonet', 'SharedGame') {
    $dir = Join-Path $packages $package
    Get-ChildItem -LiteralPath $dir -Recurse -Force |
        Where-Object { $_.Name -notlike '*.meta' -and $_.FullName -notmatch '[\\/]Samples~([\\/]|$)' -and $_.Name -ne 'Samples~' } |
        ForEach-Object { if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) { $failures.Add("Missing .meta: $($_.FullName.Substring($root.Length + 1))") } }
}

function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
$plugin = Join-Path $packages 'com.nykwil.gekkonet/Runtime/Plugins/Windows/x86_64'
$provenance = Get-Content -Raw (Join-Path $plugin 'build-provenance.json') | ConvertFrom-Json
if ((Hash (Join-Path $plugin 'GekkoNet.dll')) -ne $provenance.binarySha256) { $failures.Add('GekkoNet.dll does not match build-provenance.json binarySha256.') }
if ((Hash (Join-Path $root $provenance.patch)) -ne $provenance.patchSha256) { $failures.Add("$($provenance.patch) does not match build-provenance.json patchSha256.") }
$header = Join-Path $root 'GekkoNet/GekkoLib/include/gekkonet.h'
if (Test-Path -LiteralPath $header) {
    # Hash with LF endings so a CRLF checkout does not produce a false mismatch.
    $text = [IO.File]::ReadAllText($header).Replace("`r`n", "`n")
    $bytes = [Text.Encoding]::UTF8.GetBytes($text)
    $lf = -join ([Security.Cryptography.SHA256]::HashData($bytes) | ForEach-Object { $_.ToString('X2') })
    if ($lf -ne $provenance.headerSha256 -and (Hash $header) -ne $provenance.headerSha256) { $failures.Add('Pinned gekkonet.h does not match build-provenance.json headerSha256.') }
    $pin = (& git -C $root ls-files -s GekkoNet).Split(' ')[1]
    if ($pin -ne $provenance.revision) { $failures.Add("GekkoNet submodule pin $pin differs from provenance revision $($provenance.revision).") }
} else { Write-Output 'SKIP header/submodule checks: GekkoNet submodule not checked out.' }

$gekko = Get-Content -Raw (Join-Path $packages 'com.nykwil.gekkonet/package.json') | ConvertFrom-Json
$shared = Get-Content -Raw (Join-Path $packages 'SharedGame/package.json') | ConvertFrom-Json
if ($shared.dependencies.'com.nykwil.gekkonet' -ne $gekko.version) { $failures.Add("SharedGame depends on GekkoNet $($shared.dependencies.'com.nykwil.gekkonet') but the package is $($gekko.version).") }

if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }; exit 1 }
Write-Output "PASS package hygiene: metas, native provenance, submodule pin and dependency versions ($($gekko.name) $($gekko.version), $($shared.name) $($shared.version))"
