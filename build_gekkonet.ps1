param([string]$CMake = 'cmake', [string]$Generator = 'Visual Studio 18 2026')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ($CMake -eq 'cmake' -and $Generator -eq 'Visual Studio 18 2026') {
    $bundled = 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe'
    if (Test-Path -LiteralPath $bundled) { $CMake = $bundled }
}
& git -C $root submodule update --init GekkoNet
if ($LASTEXITCODE) { throw 'Submodule checkout failed' }
$changes = & git -C "$root/GekkoNet" status --porcelain
if ($changes) { throw 'The native submodule contains local edits. Refusing to mislabel an unpinned binary.' }
$source = "$root/build/gekkonet-source"
New-Item -ItemType Directory -Force -Path $source | Out-Null
Get-ChildItem -LiteralPath "$root/GekkoNet" -Exclude '.git' | Copy-Item -Destination $source -Recurse -Force
& git -C $root apply --directory=build/gekkonet-source "$root/gekkonet-disconnect.patch"
if ($LASTEXITCODE) { throw 'Native disconnect patch did not apply cleanly.' }
& $CMake -S $source -B "$root/build/gekkonet-release" -G $Generator -A x64 -DBUILD_SHARED_LIBS=ON -DBUILD_DOCS=OFF '-DCMAKE_CXX_FLAGS=/DWIN32 /D_WINDOWS /GR /EHsc /Brepro' -DCMAKE_SHARED_LINKER_FLAGS=/Brepro
if ($LASTEXITCODE) { throw 'CMake configuration failed' }
& $CMake --build "$root/build/gekkonet-release" --config Release
if ($LASTEXITCODE) { throw 'Native build failed' }
$destination = "$root/Unity/Packages/com.nykwil.gekkonet/Runtime/Plugins/Windows/x86_64"
New-Item -ItemType Directory -Force $destination | Out-Null
Copy-Item -LiteralPath "$root/build/gekkonet-release/GekkoLib/Release/GekkoNet.dll" -Destination $destination
$revision = & git -C "$root/GekkoNet" rev-parse HEAD
$provenance = [ordered]@{
    source = 'https://github.com/HeatXD/GekkoNet'
    revision = $revision
    patch = 'gekkonet-disconnect.patch'
    patchSha256 = (Get-FileHash -LiteralPath "$root/gekkonet-disconnect.patch").Hash
    platform = 'Windows x64'
    configuration = 'Release'
    generator = $Generator
    reproducibleFlags = '/Brepro (compiler and linker)'
    cmakeVersion = (& $CMake --version | Select-Object -First 1)
    headerSha256 = (Get-FileHash -LiteralPath "$root/GekkoNet/GekkoLib/include/gekkonet.h").Hash
    binarySha256 = (Get-FileHash -LiteralPath "$destination/GekkoNet.dll").Hash
}
$provenance | ConvertTo-Json | Set-Content -LiteralPath "$destination/build-provenance.json"
Write-Output "Built pinned GekkoNet $revision"
