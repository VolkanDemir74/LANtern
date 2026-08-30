# LANtern v0.1.7-alpha

This hotfix improves the complete update experience from discovery through relaunch.

## Fixed

- When LANtern starts in the system tray and finds an update, it now opens the control panel so the update dialog is visible.
- Update downloads now display real byte-based progress, followed by verification and installer-launch stages.
- Silent automatic updates now relaunch the installed LANtern application after setup finishes.
- The relaunched application runs as the original desktop user instead of retaining installer elevation.

## Installation behavior

LANtern downloads the official installer and checksum, verifies SHA-256, requests Windows administrator approval, installs silently, and relaunches automatically. The Windows UAC approval still requires user confirmation.

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.
