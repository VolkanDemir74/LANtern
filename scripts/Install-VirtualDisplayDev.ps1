#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $repoRoot 'x64\Debug'
$packageRoot = Join-Path $outputRoot 'IddSampleDriver'
$certificate = Join-Path $outputRoot 'IddSampleDriver.cer'
$inf = Join-Path $packageRoot 'IddSampleDriver.inf'
$log = Join-Path $repoRoot 'virtual-display-install.log'

Start-Transcript -Path $log -Force
try {
    if (-not (Test-Path -LiteralPath $certificate)) { throw "Test sertifikası bulunamadı: $certificate" }
    if (-not (Test-Path -LiteralPath $inf)) { throw "Sürücü INF dosyası bulunamadı: $inf" }

    Write-Host 'LANtern test sertifikası yükleniyor...'
    & certutil.exe -f -addstore Root $certificate
    if ($LASTEXITCODE -ne 0) { throw "Root sertifika yüklemesi başarısız: $LASTEXITCODE" }
    & certutil.exe -f -addstore TrustedPublisher $certificate
    if ($LASTEXITCODE -ne 0) { throw "TrustedPublisher sertifika yüklemesi başarısız: $LASTEXITCODE" }

    Write-Host 'LANtern sanal ekran sürücüsü Driver Store içine ekleniyor...'
    & pnputil.exe /add-driver $inf /install
    if ($LASTEXITCODE -ne 0) { throw "Sürücü kurulumu başarısız: $LASTEXITCODE" }

    Write-Host 'Kurulum tamamlandı.' -ForegroundColor Green
}
finally {
    Stop-Transcript
}
