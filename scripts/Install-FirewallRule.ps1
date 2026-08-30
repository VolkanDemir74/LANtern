$ErrorActionPreference = 'Stop'
New-NetFirewallRule -DisplayName 'DisplayOnWeb LAN HTTP' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000 -Profile Private -Program (Join-Path $PSScriptRoot '..\src\DisplayOnWeb.Host\bin\Debug\net8.0-windows10.0.19041.0\DisplayOnWeb.Host.exe')
