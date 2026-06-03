# Custom Task Manager Project Summary

Custom Task Manager is a .NET 8 WinForms app that monitors and controls Windows processes, performance, startup apps, automation rules, history, visualization, and a secured browser/mobile dashboard.

## Core Flow

1. `Program.cs` starts `MainForm`.
2. `MainForm` refreshes process/performance data roughly every 3 seconds.
3. `ProcessMonitor` reads running processes and calculates per-process CPU.
4. `NativeProcess` maps parent-child process relationships.
5. `SystemPerformanceMonitor` captures CPU, RAM, uptime, OS, battery, and GPU.
6. `StartupManager`, `RuleEngine`, and `HistoryStore` handle startup entries, automation, and CSV history.
7. `DashboardState` stores the latest snapshot for `DashboardServer`.

## Tabs

Processes:

- Live process list and tree.
- Search by name, PID, or path.
- CPU/RAM, threads, handles, status, start time, path.
- Kill selected process and open executable location.

Performance:

- CPU, memory, and GPU graphs.
- System cards for CPU, memory, GPU, battery, power, process count, thread count, handle count, uptime, logical CPUs, and OS.
- Dashboard URL/key plus open-dashboard action.

Startup:

- Registry startup entries from current-user, all-users, and 32-bit locations.
- Enable/disable with backup.

Rules:

- Process-name contains condition.
- CPU/RAM threshold.
- Alert or kill action with status and desktop notification.
- JSON storage.

Smart alerts:

- Native WinForms tray/desktop notifications.
- Sustained CPU or memory above 85% for several refreshes warns the user and names the top process.

History:

- CSV samples.
- Last 1 hour, 24 hours, and 7 days summaries.

Visualization:

- Donut/pie pressure chart using CPU, GPU, memory, battery, and process pressure.
- Overall score, status, individual cards, and recommendation.

Dashboard:

- Runs on `http://localhost:5055`.
- Same-Wi-Fi access through laptop IP and port `5055`.
- Secured API endpoint: `/api/status`.
- Access key stored in `%AppData%\CustomTaskManager\dashboard.key`.

## Why It Is Stronger Than Basic Task Manager Clone

- Adds automation rules instead of only manual process ending.
- Adds smart desktop alerts for automation and sustained system pressure.
- Tracks history to CSV instead of showing only current state.
- Uses native Windows APIs, registry access, GPU counters, and custom controls.
- Provides a secured browser/mobile dashboard.
- Demonstrates OS-level programming, not just CRUD screens.

## Main Storage

- Rules: `%AppData%\CustomTaskManager\rules.json`
- Metrics: `%AppData%\CustomTaskManager\metrics.csv`
- Dashboard key: `%AppData%\CustomTaskManager\dashboard.key`
- Startup backup: `HKCU\Software\CustomTaskManager\DisabledStartup`

## Current Notes

- `Kill` is powerful and can close child processes.
- GPU can show `Unavailable` when Windows counters cannot be read.
- Desktop notifications require the app to be running and can be affected by Windows notification settings.
- Close the running app before Debug rebuilds if the exe is locked.
