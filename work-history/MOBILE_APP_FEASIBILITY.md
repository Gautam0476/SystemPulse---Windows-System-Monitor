# Mobile App Feasibility

## Can Current App Run On Mobile?

No. It is a Windows Forms app and depends on Windows APIs, registry access, and Windows process APIs.

## Best Mobile Direction

Use the phone as a remote dashboard for the Windows laptop.

Current status:

- Windows app starts a local dashboard server on port `5055`.
- Laptop browser: `http://localhost:5055`.
- Phone on same Wi-Fi: laptop IP plus `:5055`.
- Live API: `/api/status`.
- API requires the access key shown in the Windows app.

This is strong because the laptop still performs real OS-level monitoring, while the phone becomes a viewer.

## Native Mobile App Notes

Android can show some device info, but modern Android restricts full process listing/killing for other apps unless the app has special privileges/root/device-owner access.

iOS is more restricted; full-device Task Manager style behavior is not practical for normal apps.

## Best Explanation

Current Windows app cannot directly run on mobile. For this project, mobile support should mean a secured browser/mobile dashboard that shows laptop stats over the same Wi-Fi.
