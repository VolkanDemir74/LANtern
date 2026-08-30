[CmdletBinding()]
param(
    [string]$FfmpegPath,
    [string]$MediaMtxPath,
    [switch]$DevelopmentDriver,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repoRoot 'artifacts'
$publishRoot = Join-Path $artifactsRoot 'publish\win-x64'
$stageRoot = Join-Path $artifactsRoot 'installer-stage'
$outputRoot = Join-Path $artifactsRoot 'installer'
$nativeOutput = Join-Path $artifactsRoot 'native'
$hostProject = Join-Path $repoRoot 'src\LANtern.Host\LANtern.Host.csproj'
$nativeProject = Join-Path $repoRoot 'tools\LANtern.VirtualDisplay.Device\IddSampleApp.vcxproj'
$driverProject = Join-Path $repoRoot 'drivers\LANtern.VirtualDisplay\IddSampleDriver.vcxproj'

function Find-Executable([string]$Name, [string]$ExplicitPath) {
    if ($ExplicitPath) {
        $resolved = (Resolve-Path -LiteralPath $ExplicitPath).Path
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "$Name bulunamadı: $ExplicitPath" }
        return $resolved
    }
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $wingetRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
    if (Test-Path -LiteralPath $wingetRoot) {
        $found = Get-ChildItem -LiteralPath $wingetRoot -Filter $Name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    throw "$Name bulunamadı. Yolu parametreyle verin."
}

$ffmpeg = Find-Executable 'ffmpeg.exe' $FfmpegPath
$mediamtx = Find-Executable 'mediamtx.exe' $MediaMtxPath
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe'
if (-not (Test-Path -LiteralPath $msbuild)) {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
}
if (-not $msbuild -or -not (Test-Path -LiteralPath $msbuild)) { throw 'MSBuild bulunamadı.' }

New-Item -ItemType Directory -Force -Path $nativeOutput | Out-Null
& $msbuild $nativeProject /t:Build /p:Configuration=Release /p:Platform=x64 "/p:OutDir=$nativeOutput\" /m
if ($LASTEXITCODE -ne 0) { throw 'LANtern.DeviceService derlenemedi.' }
& $msbuild $driverProject /t:Build /p:Configuration=Release /p:Platform=x64 /m
if ($LASTEXITCODE -ne 0) { throw 'LANtern sanal monitör sürücüsü derlenemedi.' }

dotnet publish $hostProject -p:PublishProfile=win-x64 --nologo
if ($LASTEXITCODE -ne 0) { throw 'LANtern publish işlemi başarısız.' }

$resolvedStage = [IO.Path]::GetFullPath($stageRoot)
if (-not $resolvedStage.StartsWith([IO.Path]::GetFullPath($artifactsRoot), [StringComparison]::OrdinalIgnoreCase)) { throw 'Geçersiz stage yolu.' }
if (Test-Path -LiteralPath $resolvedStage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $resolvedStage, (Join-Path $resolvedStage 'driver'), (Join-Path $resolvedStage 'licenses') | Out-Null
Copy-Item -Path (Join-Path $publishRoot '*') -Destination $resolvedStage -Recurse -Force
Copy-Item -LiteralPath $ffmpeg -Destination (Join-Path $resolvedStage 'ffmpeg.exe') -Force
Get-ChildItem -LiteralPath (Split-Path $ffmpeg) -Filter '*.dll' -File | Copy-Item -Destination $resolvedStage -Force
$ffmpegRoot = Split-Path (Split-Path $ffmpeg)
if (Test-Path -LiteralPath (Join-Path $ffmpegRoot 'LICENSE')) { Copy-Item -LiteralPath (Join-Path $ffmpegRoot 'LICENSE') -Destination (Join-Path $resolvedStage 'licenses\FFmpeg-LICENSE.txt') -Force }
if (Test-Path -LiteralPath (Join-Path $ffmpegRoot 'README.txt')) { Copy-Item -LiteralPath (Join-Path $ffmpegRoot 'README.txt') -Destination (Join-Path $resolvedStage 'licenses\FFmpeg-README.txt') -Force }
Copy-Item -LiteralPath $mediamtx -Destination (Join-Path $resolvedStage 'mediamtx.exe') -Force
if (Test-Path -LiteralPath (Join-Path (Split-Path $mediamtx) 'LICENSE')) { Copy-Item -LiteralPath (Join-Path (Split-Path $mediamtx) 'LICENSE') -Destination (Join-Path $resolvedStage 'licenses\MediaMTX-LICENSE.txt') -Force }
Copy-Item -LiteralPath (Join-Path $nativeOutput 'IddSampleApp.exe') -Destination (Join-Path $resolvedStage 'LANtern.DeviceService.exe') -Force
Copy-Item -Path (Join-Path $repoRoot 'drivers\LANtern.VirtualDisplay\x64\Release\IddSampleDriver\*') -Destination (Join-Path $resolvedStage 'driver') -Recurse -Force
if ($DevelopmentDriver) {
    $testCertificate = Join-Path $repoRoot 'drivers\LANtern.VirtualDisplay\x64\Release\IddSampleDriver.cer'
    if (-not (Test-Path -LiteralPath $testCertificate)) { throw 'Development driver sertifikası bulunamadı.' }
    Copy-Item -LiteralPath $testCertificate -Destination (Join-Path $resolvedStage 'driver\LANtern-Test.cer') -Force
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts\Uninstall-LANternDriver.ps1') -Destination (Join-Path $resolvedStage 'Uninstall-LANternDriver.ps1') -Force

if ($SkipInstaller) { Write-Host "Stage hazır: $resolvedStage" -ForegroundColor Green; return }
$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 bulunamadı. Önce winget install JRSoftware.InnoSetup çalıştırın.' }
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$defines = if ($DevelopmentDriver) { '/DDevelopmentDriver' } else { $null }
& $iscc $defines (Join-Path $repoRoot 'installer\LANtern.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer oluşturulamadı.' }
Write-Host "Installer hazır: $outputRoot\LANtern-Setup-x64.exe" -ForegroundColor Green
