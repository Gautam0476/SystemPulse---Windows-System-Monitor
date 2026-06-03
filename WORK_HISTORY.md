# Custom Task Manager - Work History

## Goal

Build a project-worthy Windows Task Manager style utility in .NET 8 WinForms that shows OS-level programming: processes, performance, registry startup entries, automation, history, visualization, and a browser/mobile dashboard.

## Current App

- Desktop app: `.NET 8`, `net8.0-windows`, WinForms.
- Entry point: `Program.cs`.
- Main UI: `MainForm.cs`.
- Background refresh: every ~3 seconds.
- Web dashboard: local ASP.NET Core server on port `5055`.
- Persistent data folder: `%AppData%\CustomTaskManager`.

## Implemented Features

Processes tab:

- Live process list with search by name, PID, or path.
- Process tree using parent-child process IDs.
- CPU %, RAM MB, threads, handles, status, start time, executable path.
- `Kill` selected process with child process tree after confirmation.
- `Open location` selects the executable in File Explorer.

Performance tab:

- Live CPU, memory, and GPU graphs.
- CPU, memory, GPU, battery, power, process, thread, handle, uptime, logical CPU, and OS cards.
- Dashboard URL/key display and `Open dashboard` button.

Startup tab:

- Reads current-user, all-users, and 32-bit registry startup entries.
- Disable/enable startup entries.
- Disabled startup backups stored under `HKCU\Software\CustomTaskManager\DisabledStartup`.

Rules tab:

- Custom rules by process-name match.
- CPU/RAM threshold.
- Alert or kill action, with desktop notification for alert messages.
- Rules stored in `%AppData%\CustomTaskManager\rules.json`.

Smart alerts:

- Native tray/desktop notifications through WinForms `NotifyIcon`.
- Automation-rule alerts raise a desktop notification.
- Sustained CPU or memory usage above 85% for several refreshes raises a warning and names the top process.

History tab:

- Process samples saved to `%AppData%\CustomTaskManager\metrics.csv`.
- Summary windows: last 1 hour, 24 hours, and 7 days.

Visualization tab:

- System pressure donut/pie chart.
- Inputs: CPU, GPU, memory, battery risk, process/thread/handle pressure.
- Overall score, status, metric cards, and recommendation.
- GPU uses Windows `GPU Engine` performance counters and safely shows `Unavailable` if counters cannot be read.

Browser/mobile dashboard:

- Local UI: `http://localhost:5055`.
- Same-Wi-Fi device can use laptop IP plus port.
- `/api/status` requires access key.
- Access key stored at `%AppData%\CustomTaskManager\dashboard.key`.
- Shows live CPU, memory, battery, power, process/thread/handle counts, uptime, Top CPU, and Top Memory tables.

## Recent Work

- Added secure dashboard access key and login/polished dashboard UI.
- Added Visualization tab with GPU-aware system pressure chart.
- Added GPU graph and GPU metric card to Performance tab.
- Refactored repeated WinForms layout, metric card, graph, and grid column setup into helpers to keep `MainForm.cs` more concise.
- Added UI polish: owner-drawn tabs, cleaner cards/grids, async process refresh, tree refresh optimization, and light/dark theme toggle.
- Added smart desktop alerts for automation-rule messages and sustained high CPU/RAM.
- Condensed work-history docs to reduce duplicate long notes.

## Important Files

- `MainForm.cs`: main UI, refresh flow, tab layout, commands.
- `Controls/PerformanceGraph.cs`: live line graphs.
- `Controls/SystemPieChart.cs`: visualization donut/pie chart.
- `Services/ProcessMonitor.cs`: process snapshots and CPU calculation.
- `Services/NativeProcess.cs`: parent-child process mapping.
- `Services/SystemPerformanceMonitor.cs`: CPU/RAM/system snapshot.
- `Services/GpuMonitor.cs`: GPU counter capture.
- `Services/BatteryMonitor.cs`: battery/power snapshot.
- `Services/StartupManager.cs`: registry startup read/write.
- `Services/RuleEngine.cs`: automation rules.
- `Services/HistoryStore.cs`: CSV history.
- `Services/DashboardServer.cs`: local browser/mobile dashboard and secured API.
- `Services/DashboardState.cs`: latest dashboard snapshot.
- `Models/`: simple snapshot/rule DTOs.

## Run And Verify

```powershell
cd D:\CustomTaskManager
dotnet run
```

Build check:

```powershell
dotnet build -c Release
```

Debug build can fail if the app is already running because `bin\Debug\net8.0-windows\CustomTaskManager.exe` is locked by the running process. Close the app, then rebuild.

## Known Notes

- Killing protected/system processes can fail because Windows denies permission.
- `Kill` uses `Process.Kill(entireProcessTree: true)`, so child processes can close too.
- GPU data depends on Windows GPU performance counters.
- Dashboard API without the correct key returns HTTP `401`.
- Desktop notifications depend on Windows notification/tray behavior and only show while the app is running.
- Codex cannot auto-log future conversation in the background; update `work-history/` manually after meaningful work.
