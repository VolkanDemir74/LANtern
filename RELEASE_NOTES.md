# LANtern v0.1.3-alpha

This alpha update stabilizes the 1080p streaming profile and fixes shutdown cleanup from the system tray.

## Streaming improvements

- The H.264 keyframe interval is now one second instead of four keyframes per second, reducing periodic bitrate and frame-time spikes.
- NVIDIA NVENC now uses the balanced `p3` ultra-low-latency preset.
- NVENC multipass is disabled and frame output delay is set to zero.
- The bitrate buffer has been adjusted for smoother 1080p60 delivery.
- The control panel now displays encoder FPS, processing speed, dropped frames, and duplicated frames.

## Shutdown fixes

- Exiting from the tray no longer blocks the tray interface while disconnecting the virtual monitor.
- Streaming, virtual-display, WebRTC, and child-process cleanup now run in an orderly asynchronous shutdown service.
- Cleanup operations have bounded timeouts so one component cannot leave LANtern stuck during exit.

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.

Please report problems through GitHub Issues and include the Windows version,
GPU model, browser, selected streaming profile, and the encoder telemetry shown in the control panel.
