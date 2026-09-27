$ErrorActionPreference = 'SilentlyContinue'
$drivers = pnputil.exe /enum-drivers
$driverBlocks = ($drivers -join "`n") -split "(?:`r?`n){2,}"
$publishedNames = foreach ($block in $driverBlocks) {
    if ($block -match '(?i)IddSampleDriver\.inf' -and $block -match '(?i)(oem\d+\.inf)') {
        $Matches[1]
    }
}
foreach ($publishedName in $publishedNames | Sort-Object -Unique) {
    pnputil.exe /delete-driver $publishedName /uninstall /force
}
