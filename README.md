<p align="center">
  <img src="src/DisplayOnWeb.Host/wwwroot/assets/lantern-brand.png" alt="LANtern" width="720">
</p>

<p align="center">
  <strong>Your screen, anywhere on your LAN.</strong><br>
  Use a modern browser as a low-latency display for your Windows PC over your local network.
</p>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-0078D4">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8-512BD4">
  <img alt="Streaming" src="https://img.shields.io/badge/streaming-WebRTC-FFB000">
  <img alt="Status" src="https://img.shields.io/badge/status-developer%20preview-orange">
</p>

## What is LANtern?

LANtern is a Windows host application that streams a selected display to another device on the same local network. Viewing takes place directly in a modern web browser.

The project is designed for **LAN-only** use. The current version focuses on local, view-only video streaming.

> [!IMPORTANT]
> LANtern is currently a developer preview. The streaming host and virtual display work, but the one-click installer and production driver-signing workflow are still under development.

## Highlights

- Browser-based viewing with no client installation
- Low-latency WebRTC video over the local network
- H.264 hardware encoding with NVIDIA NVENC support
- Intel Quick Sync, AMD AMF, and software H.264 fallback paths
- 1080p at up to 60 FPS streaming profile
- GPU-based Desktop Duplication capture path
- Optional mouse cursor capture without persistent cursor flicker
- LAN URL and QR code for fast mobile access
- Separate viewer and host control pages
- Responsive mobile viewer with fullscreen support
- Turkish and English control-panel interface
- Windows system-tray controls
- Optional 1920×1080 virtual monitor
- Persistent streaming and startup settings
- Automatic virtual-monitor connection and stream startup
- Child-process cleanup for FFmpeg and MediaMTX

## How it works

```text
Windows display / LANtern virtual monitor
                  │
                  ▼
        Desktop Duplication capture
                  │
                  ▼
          H.264 hardware encoder
                  │
                  ▼
           FFmpeg → local RTSP
                  │
                  ▼
             MediaMTX / WebRTC
                  │
                  ▼
             Modern browser
```

The ASP.NET Core host serves the viewer and control panel on the local network. FFmpeg captures and encodes the selected Windows display, while MediaMTX exposes the stream to the browser through WebRTC.

## Current scope

| Included | Not included yet |
|---|---|
| View-only video streaming | System audio |
| Physical display capture | Keyboard, mouse, and touch input forwarding |
| Virtual 1080p display prototype | Internet access or cloud relay |
| Hardware H.264 encoding | TURN/STUN-based remote connectivity |
| QR-based viewer access | Production-signed public driver package |
| Tray and startup automation | Final one-click installer |

## Requirements

### Host application

- Windows 10 version 1903 or newer, or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio with the **ASP.NET and web development** workload, or the .NET CLI
- FFmpeg with the required capture and H.264 encoder support
- A Windows network profile configured as **Private**

### Virtual display development

- Visual Studio with **Desktop development with C++**
- Windows SDK and Windows Driver Kit (WDK)
- x64 MSVC build tools

The virtual display driver must be signed appropriately. Development certificates are suitable for local testing only; public distribution requires a Microsoft-approved driver-signing workflow.

## Run from Visual Studio

1. Open `DisplayOnWeb.slnx` in Visual Studio.
2. Select `DisplayOnWeb.Host` as the startup project.
3. Run the HTTP profile.
4. If Windows Firewall asks for access, allow **Private networks only**.
5. Open the LANtern tray menu and select **Yönetim panelini aç / Open control panel**.
6. Connect the virtual monitor if required, select the display, and start streaming.
7. Scan the QR code or open the displayed LAN URL from another device.

The internal solution and namespace names still use `DisplayOnWeb` while the project is being migrated to the LANtern brand.

## Run from the command line

```powershell
dotnet restore .\src\DisplayOnWeb.Host\DisplayOnWeb.Host.csproj
dotnet run --project .\src\DisplayOnWeb.Host\DisplayOnWeb.Host.csproj
```

The default web server port is `5000`:

```text
Control panel: http://127.0.0.1:5000/admin.html
LAN viewer:    http://192.168.x.x:5000/
```

System-changing controls such as virtual-monitor management and persistent Windows settings are restricted to the local host control panel.

## Virtual display

LANtern includes an Indirect Display Driver prototype based on Microsoft's Indirect Display sample architecture. Windows sees it as an additional 1920×1080, 60 Hz monitor:

```text
Display 1: Physical monitor
Display 2: LANtern Virtual Monitor
```

This makes it possible to extend the Windows desktop and stream a dedicated second display. The primary monitor can keep its original resolution and content.

Driver projects are available in `DisplayOnWeb.Drivers.slnx`. Development install and uninstall scripts are located under `scripts/`.

## Settings and startup behavior

LANtern stores per-user settings under:

```text
%LocalAppData%\LANtern\settings.json
```

The control panel stores the selected display and streaming profile. Startup options can prepare the virtual monitor and saved stream automatically.

Opening the control panel at Windows startup is an optional setting and is disabled by default.

## LAN-only security model

- Do not configure router port forwarding for LANtern ports.
- Allow inbound firewall access only on the Windows **Private** profile.
- Virtual-display and persistent-setting endpoints accept requests only from loopback/localhost.
- No TURN server, cloud relay, or public discovery service is configured.
- The viewer is currently intended for trusted private networks; authentication/PIN support remains planned.

## Project structure

```text
src/DisplayOnWeb.Host/                 ASP.NET Core host, tray UI, and web client
drivers/DisplayOnWeb.VirtualDisplay/   Windows Indirect Display Driver
tools/DisplayOnWeb.VirtualDisplay.Device/  Virtual-display device helper
scripts/                               Development install and firewall scripts
```

## Roadmap

- [x] LAN web server and browser viewer
- [x] WebRTC/H.264 streaming pipeline
- [x] NVIDIA NVENC low-latency path
- [x] Responsive viewer and fullscreen controls
- [x] QR viewer link
- [x] Tray application and persistent settings
- [x] 1080p virtual display prototype
- [ ] Single-file Windows installer and clean uninstaller
- [ ] Production driver signing and packaging
- [ ] PIN-based viewer authentication
- [ ] mDNS discovery (`lantern.local`)
- [ ] WASAPI loopback audio
- [ ] Optional input forwarding

## Contributing

Issues and pull requests are welcome. Streaming problem reports should include:

- Windows version
- GPU and driver version
- Browser and client device
- Selected streaming profile
- Whether a physical or LANtern virtual display was used
- Relevant host logs with private network details removed

## Author

Created by **Volkan Demir**  
[github.com/VolkanDemir74](https://github.com/VolkanDemir74)

## Türkçe kısa açıklama

LANtern, Windows ekranını aynı yerel ağdaki modern tarayıcılara düşük gecikmeyle aktaran açık kaynak olarak yayımlanması planlanan bir projedir. Telefon, tablet veya başka bir bilgisayara istemci uygulaması kurmak gerekmez. Proje yalnızca güvenilir yerel ağ kullanımı için tasarlanmıştır.

Geliştirme kurulumu, özellikler ve güvenlik ayrıntıları için yukarıdaki İngilizce belgelendirmeyi inceleyebilirsiniz. Türkçe arayüz uygulamanın yönetim panelinden seçilebilir.

## License

LANtern is released under the [MIT License](LICENSE). Third-party components remain subject to their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
