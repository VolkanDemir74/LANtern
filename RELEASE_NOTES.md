# LANtern v0.1.6-alpha

This hotfix corrects native control-panel startup for installed copies of LANtern.

## Fixed

- WebView2 profile data is now stored under the current user's local application-data folder instead of the protected Program Files installation directory.
- Installed copies no longer fail with `E_ACCESSDENIED` when opening the native control panel as a standard user.
- If the embedded WebView2 panel cannot start for another reason, LANtern now reports the problem and opens the control panel in the default browser instead of terminating with an unhandled exception.

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.
