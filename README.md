<p align="center">
  <img src="src/LANtern.Host/wwwroot/assets/lantern-brand.png" alt="LANtern" width="720">
</p>

<p align="center">
  <strong>Your screen, anywhere on your LAN.</strong><br>
  Add a virtual Windows monitor and view it from any modern browser on your local network.
</p>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-0078D4">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8-512BD4">
  <img alt="Streaming" src="https://img.shields.io/badge/streaming-WebRTC-FFB000">
  <img alt="Release" src="https://img.shields.io/badge/release-v0.1.6--alpha-orange">
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green">
</p>

## What is LANtern?

LANtern turns a phone, tablet, laptop, or desktop browser into a dedicated display for a Windows PC. Its virtual display driver adds a real 1920x1080 monitor to Windows, which can be extended, arranged, and used like another screen. LANtern captures that monitor and delivers it to the browser through low-latency WebRTC video.

An existing physical monitor can also be streamed when a virtual display is not needed. LANtern is designed for **LAN-only**, view-only use on trusted private networks.

## No viewer app required

Install LANtern only on the Windows host PC. Phones, tablets, laptops, smart displays, and other viewing devices connect through a current web browser. There is no client application to download, no account, no subscription, and no session time limit.

LANtern is completely free and open source under the MIT License. The source code, Windows installer, and release checksums are published in this repository.

> [!IMPORTANT]
> LANtern is currently an alpha release intended for testing on trusted private networks. The installer contains a development-signed virtual display driver. Windows may display an unknown publisher or certificate warning.

## Install LANtern

Download `LANtern-Setup-x64.exe` from [GitHub Releases](https://github.com/VolkanDemir74/LANtern/releases) and run it as an administrator. The installer includes the Windows host, browser interface, FFmpeg, MediaMTX, virtual display service, and development-signed driver.

The setup wizard asks only once, on its final page, whether LANtern should be launched after installation.

After installation:

1. Start LANtern from the Start menu.
2. Open the tray icon and select **Open control panel**.
3. Connect the virtual monitor, or select an existing physical display.
4. Start the stream.
5. Scan the QR code with the viewing device.

The viewing device only needs a current web browser and access to the same private Wi-Fi or Ethernet network. Nothing is installed on the viewing device.

## Highlights

- Adds a 1920x1080, 60 Hz virtual monitor to Windows
- Extends the Windows desktop onto a browser device
- Keeps the physical monitor free for other applications
- Browser-based viewing with no client installation
- Low-latency WebRTC video over the local network
- H.264 hardware encoding with NVIDIA NVENC support
- Automatic runtime encoder detection with NVENC, Quick Sync, AMD, and software fallback
- Optional mouse-cursor capture across all encoder paths
- Live encoder FPS, speed, dropped-frame, and duplicated-frame telemetry
- Intel Quick Sync, AMD AMF, and software H.264 fallback paths
- 1080p at up to 60 FPS streaming profile
- 10 Mbps low-latency bitrate selected by default, with higher profiles available
- GPU-based Desktop Duplication capture path
- Optional mouse cursor capture without persistent cursor flicker
- LAN URL and QR code for fast mobile access
- Separate viewer and host control pages
- Responsive mobile viewer with fullscreen support
- Turkish and English control-panel interface
- Windows system-tray controls
- Single-instance host with a dedicated WebView2 control-panel window
- Selectable native WebView2, Microsoft Edge, or Google Chrome control-panel client
- Proportional native control-panel sizing tuned for ultrawide and standard desktops
- Native dark title bar matching the LANtern interface
- Compact state-aware controls for streaming and the virtual monitor
- Collapsible QR access beside the plain LAN viewer address
- Compact two-column settings with native-style toggle switches
- Physical display streaming when a virtual monitor is not needed
- Persistent streaming and startup settings
- Automatic persistence of the display used by the most recent successful stream
- Secure update checks through official GitHub Releases, with manual and startup controls
- Automatic virtual-monitor connection and stream startup
- Child-process cleanup for FFmpeg and MediaMTX
- Clean stream restart and shutdown without stale FFmpeg pipe errors

## More than screen sharing

Traditional screen-sharing tools mirror content that already exists on a physical display. LANtern can create an additional monitor inside Windows and stream that separate desktop area to another device.

Windows applications can be moved to the LANtern virtual monitor while the primary display remains independent. This makes a browser device useful as a dedicated secondary screen for dashboards, communication tools, documents, media, or any other extended-desktop workflow.

## How it works

```text
     LANtern virtual display driver
                  │
                  ▼
       Windows adds Display 2
                  │
                  ▼
LANtern virtual monitor / physical display
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
| Virtual 1080p Windows display | Internet access or cloud relay |
| Hardware H.264 encoding | TURN/STUN-based remote connectivity |
| QR-based viewer access | Production-signed public driver package |
| Tray, installer, and startup automation | Production code-signed installer |

## Requirements

### Installed application

- Windows 10 version 1903 or newer, or Windows 11
- x64 processor
- A current Chrome, Edge, Safari, or Firefox browser on the viewing device
- A Windows network profile configured as **Private**

The installer includes the .NET runtime and streaming components. Visual Studio and the .NET SDK are only needed when building the source code.

### Source development

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio with the **ASP.NET and web development** workload
- FFmpeg and MediaMTX for local development

### Virtual display development

- Visual Studio with **Desktop development with C++**
- Windows SDK and Windows Driver Kit (WDK)
- x64 MSVC build tools

The current alpha package uses a development certificate. A stable public package requires a Microsoft-approved driver-signing workflow.

## Run from Visual Studio

1. Open `LANtern.slnx` in Visual Studio.
2. Select `LANtern.Host` as the startup project.
3. Run the HTTP profile.
4. If Windows Firewall asks for access, allow **Private networks only**.
5. Open the LANtern tray menu and select **Yönetim panelini aç / Open control panel**.
6. Connect the virtual monitor if required, select the display, and start streaming.
7. Scan the QR code or open the displayed LAN URL from another device.

## Run from the command line

```powershell
dotnet restore .\src\LANtern.Host\LANtern.Host.csproj
dotnet run --project .\src\LANtern.Host\LANtern.Host.csproj
```

The default web server port is `5000`:

```text
Control panel: http://127.0.0.1:5000/admin.html
LAN viewer:    http://192.168.x.x:5000/
```

System-changing controls such as virtual-monitor management and persistent Windows settings are restricted to the local host control panel.

## Build the installer

Run the packaging script from an elevated PowerShell terminal after installing Inno Setup 6 and the driver development requirements:

```powershell
.\scripts\Build-Installer.ps1 -DevelopmentDriver
```

The generated package is written to:

```text
artifacts\installer\LANtern-Setup-x64.exe
```

## Virtual Windows monitor

LANtern includes an Indirect Display Driver based on Microsoft's Indirect Display architecture. Windows sees it as an additional 1920x1080, 60 Hz monitor:

```text
Display 1: Physical monitor
Display 2: LANtern Virtual Monitor
```

The Windows desktop can be extended onto this monitor just like a connected physical screen. LANtern then captures only the virtual monitor and sends it to the browser. The primary monitor keeps its own resolution, applications, and fullscreen content.

The virtual monitor can be connected or disconnected from the control panel and tray menu. Startup settings can connect it automatically and begin streaming with the saved profile.

Driver projects are available in `LANtern.Drivers.slnx`. Development install and uninstall scripts are located under `scripts/`.

## Settings and startup behavior

LANtern stores per-user settings under:

```text
%LocalAppData%\LANtern\settings.json
```

The control panel stores the selected display and streaming profile. Startup options can prepare the virtual monitor and saved stream automatically.

LANtern opens the control panel when the application starts. The optional **Start in tray** setting keeps the panel closed and starts LANtern in the notification area instead.

LANtern runs as a Windows tray application without opening a command window.

A short tray notification confirms that LANtern is running in the background when **Start in tray** is enabled. Exiting LANtern runs an orderly shutdown that stops the stream, closes its child processes, disconnects clients, and removes the active virtual monitor. The device service remains available for the next launch and is removed by the uninstaller.

LANtern can check the official GitHub Releases page when the control panel opens. Available updates can be installed immediately, postponed, skipped for that version, or disabled. The Settings panel also provides a manual update check. Downloaded installers are accepted only when their SHA-256 checksum matches the checksum published with the release.

## LAN-only security model

- Do not configure router port forwarding for LANtern ports.
- Allow inbound firewall access only on the Windows **Private** profile.
- Virtual-display and persistent-setting endpoints accept requests only from loopback/localhost.
- No TURN server, cloud relay, or public discovery service is configured.
- The viewer is currently intended for trusted private networks; authentication/PIN support remains planned.

## Official releases and forks

Official LANtern releases are published only through the [VolkanDemir74/LANtern](https://github.com/VolkanDemir74/LANtern) repository. Release installers include a checksum file so downloaded packages can be verified.

Forks and third-party builds are maintained independently. They are not reviewed, endorsed, signed, or supported by the LANtern project unless explicitly stated in this repository. Users should inspect the source and publisher before installing a driver or installer from another location.

## Project structure

```text
src/LANtern.Host/                 ASP.NET Core host, tray UI, and web client
drivers/LANtern.VirtualDisplay/   Windows Indirect Display Driver
tools/LANtern.VirtualDisplay.Device/  Virtual-display device helper
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
- [x] Windows installer and uninstaller
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

## Development note

AI-assisted tools were used in some parts of the development process. Project direction, design decisions, testing, and final review are maintained by the author.

Projenin geliştirme sürecindeki bazı çalışmalarda yapay zeka destekli araçlardan yararlanılmıştır. Proje yönü, tasarım kararları, testler ve son kontroller geliştirici tarafından yürütülmektedir.

## Türkçe kısa açıklama

LANtern, Windows'a 1920x1080 sanal bir monitör ekler ve bu monitörü aynı yerel ağdaki modern tarayıcılarda düşük gecikmeyle görüntüler. Böylece telefon, tablet veya başka bir bilgisayar yalnızca ekran yansıtmakla kalmaz, Windows'un bağımsız ikinci ekranı olarak kullanılabilir. İzleme cihazına uygulama kurulması gerekmez.

LANtern yalnızca Windows ana bilgisayarına kurulur. İzleme cihazında güncel bir web tarayıcısı yeterlidir. İstemci uygulaması, hesap, abonelik veya oturum süresi sınırı yoktur. LANtern tamamen ücretsiz ve açık kaynaklıdır.

İstenirse mevcut fiziksel monitörlerden biri de yayınlanabilir. LANtern yalnızca güvenilir yerel ağlarda, görüntüleme amaçlı kullanım için tasarlanmıştır.

Geliştirme kurulumu, özellikler ve güvenlik ayrıntıları için yukarıdaki İngilizce belgelendirmeyi inceleyebilirsiniz. Türkçe arayüz uygulamanın yönetim panelinden seçilebilir.

## License

LANtern is released under the [MIT License](LICENSE). Third-party components remain subject to their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
