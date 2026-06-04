# SystemPulse - Windows System Monitor

SystemPulse is a Windows desktop system monitor built with C#, .NET 8, and Windows Forms.

## Features

- Live process list with CPU, RAM, PID, parent PID, threads, handles, status, and executable path
- Process search and process tree view
- Safe process kill confirmation
- Smart RAM Advisor for high-memory user apps
- CPU, RAM, GPU, battery, uptime, and system performance view
- Startup app manager
- CPU/RAM automation rules with alert or kill actions
- Process history and visualization
- Secured local browser/mobile dashboard on port `5055`

## Download

Download the ready-to-run ZIP from the GitHub releases page:

https://github.com/Gautam0476/SystemPulse---Windows-System-Monitor/releases

Extract the ZIP and run `CustomTaskManager.exe`.

## Build From Source

Requirements:

- Windows
- .NET 8 SDK

Run:

```powershell
dotnet build -c Release
dotnet run
```

## Notes

- Windows SmartScreen may warn because the app is not digitally signed.
- Some protected system processes may not expose all details without permission.
- GPU data depends on Windows GPU performance counters.
- For mobile dashboard access, keep the app open and connect the phone and laptop to the same Wi-Fi.
