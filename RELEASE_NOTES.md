# LANtern v0.1.5-alpha

This alpha release introduces a dedicated native control panel, secure update checks, clearer automation settings, and several streaming stability improvements.

## Native control panel

- The control panel now opens in a dedicated WebView2 window instead of creating a browser tab.
- Native, Microsoft Edge, and Google Chrome panel clients can be selected in Settings.
- The native window uses a dark title bar, adaptive sizing, and system-tray minimize behavior.
- LANtern is now single-instance. Launching it again activates the existing control-panel window.

## Updates

- LANtern can check the official `VolkanDemir74/LANtern` GitHub Releases feed at startup or on demand.
- Available releases can be installed now, postponed, skipped, or disabled.
- Downloaded installers are verified against the SHA-256 checksum published with the release before execution.

## Interface

- The control sidebar has a compact responsive layout with state-aware stream and virtual-monitor buttons.
- Settings use grouped two-column controls and toggle switches with clearer automation descriptions.
- The LAN address is presented as plain text with a collapsible vector QR button.
- Turkish and English interface text has been updated throughout.
- The default native window and settings dialog better fit standard and ultrawide desktops.

## Streaming and lifecycle

- The default streaming bitrate is now 10 Mbps for lower latency. Higher bitrate profiles remain available.
- The display used by the most recent successful stream is remembered automatically.
- Stale FFmpeg broken-pipe errors no longer replace the current stream status.
- Stream, WebRTC gateway, virtual monitor, and child processes continue to use orderly shutdown cleanup.

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.

Please report problems through GitHub Issues and include the Windows version, GPU model, selected display and encoder, browser or panel client, and any error shown in the control panel.
