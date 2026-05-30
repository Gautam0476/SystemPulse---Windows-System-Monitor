# Mobile App Feasibility

Date: 29 May 2026

## Can The Current Windows App Run On Mobile?

No. Current app is a Windows Forms desktop app.

It uses:

- WinForms
- Windows Registry
- Windows native APIs
- `System.Diagnostics.Process` for Windows processes

So the current `.exe` or `.zip` will run on Windows laptops/desktops only, not Android/iPhone.

## Option 1: Mobile As Remote Dashboard For Laptop

This is the best project direction.

Current status:

- Basic version implemented on 29 May 2026.
- Windows app starts a local dashboard server on port `5055`.
- Laptop browser can open `http://localhost:5055`.
- Phone on same Wi-Fi can open the laptop IP plus port, example `http://192.168.1.14:5055`.
- Dashboard API endpoint: `http://localhost:5055/api/status`.
- Dashboard live API now requires the access key shown in the Windows app.

How it works:

- Windows app runs on laptop.
- Windows app exposes local API or web dashboard.
- Phone opens browser/app and sees laptop CPU/RAM/process status.

Example:

- Laptop app running at `http://192.168.1.5:5000`
- Mobile browser opens that address.
- Phone can view laptop performance and maybe send actions.

Why strong:

- Clearly different from normal Task Manager.
- Useful and interview-friendly.
- Avoids mobile OS restrictions because actual laptop monitoring still happens on the laptop.

## Option 2: Mobile App To Monitor Mobile's Own RAM/CPU

This requires a separate mobile app.

Possible for Android:

- Android native app using Kotlin/Java
- .NET MAUI Android app
- Flutter/React Native with native plugins

Limitations:

- Modern Android does not allow normal apps to freely view/kill every other app's process like Windows Task Manager.
- It can show device RAM, storage, battery, app memory, and some system info.
- Full process list/kill features usually need root, device-owner mode, or special privileged permissions.

For iPhone/iOS:

- Very restricted.
- Apps cannot work like full Task Manager for the whole phone.
- Mostly only own-app metrics are available.

## Option 3: One Codebase With .NET MAUI

.NET MAUI can create cross-platform apps, but current WinForms UI cannot be directly converted.

Possible approach:

- Keep shared models/business logic where possible.
- Create separate UI in .NET MAUI.
- Write separate OS-specific services:
  - Windows service for Windows process details
  - Android service for Android device info
  - iOS limited service for iOS info

## Best Recommendation

For this project, the strongest next step is:

> Add a mobile-friendly remote dashboard for the Windows laptop.

Reason:

- Phone support mil jaayega.
- Windows app ka current OS-level monitoring reuse hoga.
- Inbuilt Task Manager se clear difference banega.
- Android/iOS restrictions ka issue kam hoga.

## Simple Explanation

Current Windows app mobile par directly nahi chalegi.

Mobile support ke liye two choices:

1. Phone ko remote viewer bana do for laptop stats.
2. Mobile ke liye alag Android app banao, but mobile OS restrictions ke kaaran features limited honge.
