#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$device = Get-PnpDevice -PresentOnly:$false | Where-Object InstanceId -Like '*DisplayOnWebVirtualDisplay*'
foreach ($item in $device) {
    & pnputil.exe /remove-device $item.InstanceId
}

$driver = pnputil.exe /enum-drivers /class Display
Write-Host 'Gerekirse Aygıt Yöneticisi üzerinden DisplayOnWeb Virtual Monitor sürücü paketini kaldırın.'
