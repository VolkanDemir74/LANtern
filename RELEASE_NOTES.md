# LANtern v0.1.8-alpha

This alpha release improves stream resilience and makes the running application version visible throughout the interface.

## Stream recovery

- Added an optional setting to restart streaming after an unexpected capture-process stop.
- LANtern remembers the last successful display and streaming profile for recovery.
- Recovery retries begin after a short delay and back off to a maximum interval of 15 seconds when failures continue.
- Manual stream stop, virtual-monitor disconnection, and application shutdown cancel recovery immediately.

## Version visibility

- The running version is displayed in the lower-left corner of the control panel.
- The tray About dialog now displays the same assembly version.
- Native control-panel URLs include the application version so WebView2 cannot reuse stale interface files after an update.

## Important notes

- Borderless fullscreen remains recommended for uninterrupted capture. Exclusive fullscreen transitions can invalidate Windows Desktop Duplication and trigger automatic recovery.
- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.
