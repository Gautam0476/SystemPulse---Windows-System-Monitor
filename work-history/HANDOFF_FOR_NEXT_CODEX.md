# Handoff For Next Codex

Project: `D:\CustomTaskManager`

## User Context

User is building a resume-worthy Custom Task Manager in Hinglish. Keep explanations simple, direct, and practical. The user often asks what each UI button/feature does, then asks for the project to be improved.

## Current State

The app is a .NET 8 WinForms desktop utility and builds successfully with:

```powershell
dotnet build -c Release
```

Run it with:

```powershell
cd D:\CustomTaskManager
dotnet run
```

If Debug build fails with an exe lock, close the currently running Custom Task Manager window and rebuild.

## Feature Map

- Processes: live list, search, tree, CPU/RAM, threads, handles, status, start time, path.
- Actions: refresh, open executable location, kill selected process after confirmation.
- Performance: CPU, memory, and GPU graphs plus system metric cards.
- Startup: registry startup entries with enable/disable and backup.
- Rules: process-name match with CPU/RAM threshold, alert, or kill.
- History: CSV process metrics and 1h/24h/7d summaries.
- Visualization: CPU/GPU/memory/battery/process pressure pie chart and recommendation.
- Dashboard: local ASP.NET Core page/API on port `5055`, protected by an access key.
- Smart alerts: tray/desktop notifications for automation alerts and sustained high CPU/RAM.

## Key Files

- `MainForm.cs`: primary UI, refresh loop, tab building, event handlers.
- `Controls/PerformanceGraph.cs`: CPU/memory/GPU line graph control.
- `Controls/SystemPieChart.cs`: visualization chart.
- `Services/ProcessMonitor.cs`: process snapshots.
- `Services/NativeProcess.cs`: parent PID lookup.
- `Services/SystemPerformanceMonitor.cs`: CPU/RAM/system metrics.
- `Services/GpuMonitor.cs`: Windows GPU Engine counters.
- `Services/StartupManager.cs`: startup registry logic.
- `Services/RuleEngine.cs`: automation behavior.
- `Services/HistoryStore.cs`: CSV metrics.
- `Services/DashboardServer.cs`: secured browser/mobile dashboard.
- `Models/`: snapshot and rule data types.

## Persistent Data

- Rules: `%AppData%\CustomTaskManager\rules.json`
- History: `%AppData%\CustomTaskManager\metrics.csv`
- Dashboard key: `%AppData%\CustomTaskManager\dashboard.key`
- Disabled startup backup: `HKCU\Software\CustomTaskManager\DisabledStartup`

## Recent Changes

- Added GPU to Performance tab as a graph and metric card.
- Refactored repeated layout, graph, metric card, and grid-column setup in `MainForm.cs`.
- Added `ValueTextOverride` to `PerformanceGraph` so unavailable GPU can display as `Unavailable`.
- Added UI polish with owner-drawn tabs, refined colors, async process refresh, process-tree refresh optimization, and a light/dark theme toggle.
- Added native WinForms `NotifyIcon` desktop notifications for automation-rule alerts plus sustained high CPU/RAM smart alerts.
- Condensed work-history docs to remove duplicate long history while preserving handoff details.

## Gotchas

- `Kill` uses `Process.Kill(entireProcessTree: true)`, so child processes can be closed.
- Protected/system process killing can fail due to Windows permissions.
- GPU display depends on Windows `GPU Engine` performance counters.
- Dashboard `/api/status` returns `401` without the correct key.
- Desktop notifications only appear while the app is running, and Windows notification settings can suppress balloon display.
- Keep `work-history/` updated after meaningful future work; Codex cannot log automatically in the background.
