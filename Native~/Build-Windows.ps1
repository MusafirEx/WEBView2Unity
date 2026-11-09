# WEBView2Unity CEF Windows x64 native build and runtime installer.
# Run manually in Windows PowerShell with VS2022 C++ tools, CMake and tar.
[CmdletBinding()]
param(
    [string]$Version = "154.0.34+g14c5a08+chromium-154.0.8037.98",
    [ValidateSet("minimal", "standard")][string]$Distribution = "minimal",
    [string]$CefRoot = "",
    [string]$WorkDirectory = (Join-Path $env:LOCALAPPDATA "WEBView2Unity\BuildCache"),
    [string]$OutputDirectory = (Join-Path $env:LOCALAPPDATA "WEBView2Unity\Runtime")
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-ExitCode([string]$Name) {
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE" }
}
if ($PSVersionTable.PSEdition -eq "Core" -and -not $IsWindows) {
    throw "This build currently supports Windows x64 only."
}
if (-not [Environment]::Is64BitOperatingSystem) { throw "Windows x64 is required." }
if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    throw "CMake is required. Install VS 2022 Desktop development with C++ and CMake."
}
if (-not (Get-Command tar -ErrorAction SilentlyContinue)) { throw "The Windows tar utility is required." }
New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
$WorkDirectory = (Resolve-Path $WorkDirectory).Path

if ([string]::IsNullOrWhiteSpace($CefRoot)) {
    $suffix = if ($Distribution -eq "minimal") { "_minimal" } else { "" }
    $base = "cef_binary_$($Version)_windows64$suffix"
    $archive = "$base.tar.bz2"
    $archivePath = Join-Path $WorkDirectory $archive
    $checksumPath = "$archivePath.sha1"
    $remote = "https://cef-builds.spotifycdn.com/" + [Uri]::EscapeDataString($archive)
    $CefRoot = Join-Path $WorkDirectory $base
    Write-Host "CEF: $base"
    if (-not (Test-Path $checksumPath)) {
        Invoke-WebRequest -Uri "$remote.sha1" -OutFile $checksumPath -UseBasicParsing
    }
    $expected = ((Get-Content $checksumPath -Raw).Trim() -split "\s+")[0].ToLowerInvariant()
    if ($expected -notmatch "^[a-f0-9]{40}$") {
        throw "Unexpected upstream SHA1 file; refusing to install unverified archive."
    }
    $validArchive = $false
    if (Test-Path $archivePath) {
        $validArchive = ((Get-FileHash $archivePath -Algorithm SHA1).Hash.ToLowerInvariant() -eq $expected)
    }
    if (-not $validArchive) {
        Write-Host "Downloading official CEF distribution from $remote"
        Invoke-WebRequest -Uri $remote -OutFile $archivePath -UseBasicParsing
        $actual = (Get-FileHash $archivePath -Algorithm SHA1).Hash.ToLowerInvariant()
        if ($actual -ne $expected) {
            Remove-Item -LiteralPath $archivePath -Force
            throw "CEF archive checksum mismatch: expected $expected, got $actual."
        }
    }
    if (-not (Test-Path (Join-Path $CefRoot "include\cef_app.h"))) {
        Write-Host "Extracting CEF archive..."
        & tar -xjf $archivePath -C $WorkDirectory
        Assert-ExitCode "CEF extraction"
    }
} else {
    $CefRoot = [IO.Path]::GetFullPath($CefRoot)
    $expected = "Provided local CEF SDK"
}
if (-not (Test-Path (Join-Path $CefRoot "include\cef_app.h")) -or
    -not (Test-Path (Join-Path $CefRoot "Release\libcef.lib"))) {
    throw "CEF SDK is incomplete: $CefRoot"
}
$buildDir = Join-Path $WorkDirectory "NativeBuild"
Write-Host "Configuring bridge using CEF at $CefRoot"
& cmake -S $PSScriptRoot -B $buildDir -G "Visual Studio 17 2022" -A x64 "-DCEF_ROOT=$CefRoot"
Assert-ExitCode "CMake configure"
& cmake --build $buildDir --config Release --parallel 4
Assert-ExitCode "Native C++ build"
$bridgeDll = Join-Path $buildDir "Release\WEBView2UnityCEF.dll"
$helperExe = Join-Path $buildDir "Release\WEBView2UnityCEFSubprocess.exe"
if (-not (Test-Path $bridgeDll) -or -not (Test-Path $helperExe)) {
    throw "Native output missing. Inspect the CMake build output."
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $CefRoot "Release") -File)) {
    if ($file.Extension -in @(".dll", ".bin", ".pak", ".dat")) {
        Copy-Item -LiteralPath $file.FullName -Destination $OutputDirectory -Force
    }
}
$resourcesPath = Join-Path $CefRoot "Resources"
if (Test-Path $resourcesPath) {
    foreach ($file in (Get-ChildItem -LiteralPath $resourcesPath -Force)) {
        Copy-Item -LiteralPath $file.FullName -Destination $OutputDirectory -Recurse -Force
    }
}
foreach ($file in @("LICENSE.txt", "README.txt", "LICENSES.chromium.html")) {
    $source = Join-Path $CefRoot $file
    if (Test-Path $source) { Copy-Item -LiteralPath $source -Destination $OutputDirectory -Force }
}
Copy-Item -LiteralPath $bridgeDll -Destination $OutputDirectory -Force
Copy-Item -LiteralPath $helperExe -Destination $OutputDirectory -Force
foreach ($required in @("libcef.dll", "WEBView2UnityCEF.dll", "WEBView2UnityCEFSubprocess.exe")) {
    if (-not (Test-Path (Join-Path $OutputDirectory $required))) {
        throw "Runtime deployment incomplete: $required"
    }
}
@"
CEF version: $Version
Source: $CefRoot
Upstream archive SHA1: $expected
Target: Windows x64
Status: experimental, CEF sandbox disabled; not suitable for sensitive authentication.
"@ | Set-Content -LiteralPath (Join-Path $OutputDirectory "VERSION.txt") -Encoding UTF8
Write-Host ""
Write-Host "Build output ready: $OutputDirectory" -ForegroundColor Green
Write-Host "In Unity: Window > WEBView2Unity > Browser"
Write-Host "Select the directory above as 'CEF Runtime Folder', then click Initialize."
Write-Warning "Experimental native engine; no security sandbox. Test example.com only."
