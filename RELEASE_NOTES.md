# LANtern v0.1.4-alpha

This alpha update improves compatibility across different GPUs and simplifies first-time installation.

## Encoder compatibility

- Automatic encoder selection now performs a real test encode instead of relying only on the FFmpeg encoder list.
- LANtern tries NVIDIA NVENC, Intel Quick Sync, AMD AMF, and software H.264 in preference order.
- Computers without a supported hardware encoder now fall back to software H.264 automatically.
- Manually selecting an unavailable encoder now produces a clear message in the control panel.
- FFmpeg startup failures are shown in the control panel instead of referring to a hidden console.

## Cursor capture

- The **Show mouse cursor** setting now works with Intel Quick Sync, AMD AMF, and software H.264 capture paths.
- Cursor visibility remains configurable for every encoder.

## Installer

- The setup wizard now asks only once whether LANtern should launch after installation.
- The installer remains self-contained and includes the host, FFmpeg, MediaMTX, virtual-display service, and development-signed driver.

## Important notes

- This release is intended for testing on trusted private networks.
- The included virtual display driver uses a development certificate.
- Windows and SmartScreen may show an unknown publisher warning.
- Viewer authentication has not been implemented yet.
- A production-signed driver will be required for a stable public release.

Please report problems through GitHub Issues and include the Windows version,
GPU model, browser, selected encoder, and any error shown in the control panel.
