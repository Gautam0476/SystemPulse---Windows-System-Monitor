# Non-Overlapping Chat History

Compact project history without repeated long entries.

## Start

User wanted a custom Task Manager/process monitor that demonstrates OS-level .NET work and looks stronger than a basic CRUD app.

Initial project:

- `D:\CustomTaskManager`
- Basic console app.
- Converted to `.NET 8` WinForms with `UseWindowsForms`.

## Built App

Core files added:

- `Program.cs`
- `MainForm.cs`
- `Models/*`
- `Services/*`
- `Controls/PerformanceGraph.cs`
- `Controls/SystemPieChart.cs`
- `Controls/PieChartSlice.cs`

Core services:

- `ProcessMonitor`: process snapshots and CPU.
- `NativeProcess`: parent-child process tree.
- `StartupManager`: startup registry entries.
- `RuleStore`/`RuleEngine`: automation rules.
- `HistoryStore`: CSV history.
- `SystemPerformanceMonitor`: CPU/RAM/system metrics.
- `BatteryMonitor`: battery/power.
- `GpuMonitor`: GPU Engine counters.
- `DashboardServer`/`DashboardState`: secured browser/mobile dashboard.

## Features Completed

- Processes tab with search, tree, CPU/RAM, threads, handles, status, start time, path.
- Kill selected process with confirmation and self-kill protection.
- Open selected process executable location.
- Startup manager with enable/disable.
- Automation rules with alert/kill.
- History summaries for 1h/24h/7d.
- Performance tab with CPU, memory, and GPU graphs/cards.
- Secured dashboard on port `5055`.
- Visualization tab with overall pressure chart and recommendation.

## Bugs/Fixes

Startup crash:

- Cause: `SplitContainer.SplitterDistance` set before valid width.
- Fix: configure splitter after handle creation/resize.

Dashboard auth:

- API now requires access key.
- Missing/wrong key returns `401`.

GPU UI gap:

- GPU was captured but not shown in Performance tab.
- Added GPU graph and card.

## Latest Cleanup

- Refactored repeated WinForms UI setup into helpers.
- Condensed work-history docs to reduce duplication.

## Verification Pattern

Use:

```powershell
dotnet build -c Release
```

Close running app before Debug rebuild if exe is locked.
