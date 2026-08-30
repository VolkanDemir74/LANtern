$ErrorActionPreference = 'SilentlyContinue'
$drivers = pnputil.exe /enum-drivers
$publishedName = $null
for ($i = 0; $i -lt $drivers.Count; $i++) {
    if ($drivers[$i] -match 'IddSampleDriver\.inf') {
        for ($j = $i; $j -ge [Math]::Max(0, $i - 5); $j--) {
            if ($drivers[$j] -match '(oem\d+\.inf)') { $publishedName = $Matches[1]; break }
        }
    }
}
if ($publishedName) { pnputil.exe /delete-driver $publishedName /uninstall /force }
