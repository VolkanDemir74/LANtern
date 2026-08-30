# LANtern v0.1.2-alpha

This alpha update completes the LANtern product rename and improves application, tray, and virtual-display lifecycle behavior.

## Improved

- The project, executable, solutions, namespaces, driver, and virtual-display device now consistently use the LANtern name.
- LANtern opens the control panel by default on a normal launch.
- The optional **Start in tray** setting keeps the panel closed.
- When starting in tray, LANtern displays a clickable startup card near the notification area.
- The tray tooltip clearly indicates that LANtern is running in the background.
- Closing LANtern now stops streaming, disconnects the active virtual monitor, and cleans up child processes.
- Virtual-display service installation and removal are handled by the native LANtern device helper.

## Included

- Local-network browser viewer
- WebRTC video streaming through MediaMTX
- H.264 hardware encoding support
- Physical display capture
- 1920x1080 virtual Windows monitor
- Windows tray controls
- Turkish and English control panel
- QR viewer link
- Persistent streaming settings
- Optional Windows startup automation
- Native Windows service for virtual display control

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.

Please report problems through GitHub Issues and include the Windows version,
GPU model, browser, and selected streaming profile.
