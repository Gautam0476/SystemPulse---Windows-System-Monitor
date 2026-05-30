# Non-Overlapping Chat History

Purpose: Ye file poori chat/project history ko duplicate entries ke bina compact form mein preserve karti hai. Next Codex isko read karke conversation flow samajh sakta hai.

## 1. Project Idea

User ne custom Task Manager / Process Monitor project banana tha.

Original expected features:

- Process tree
- Process killer
- Startup manager
- Custom metrics/history
- Automation rules
- OS-level utility using .NET

User wanted project to look stronger than basic CRUD/web app and show OS-level programming.

## 2. Initial Workspace

Project path:

- `D:\CustomTaskManager`

Initial files:

- `Program.cs` with `Hello, World!`
- `CustomTaskManager.csproj` as console app

.NET status:

- .NET SDK 8 installed
- Windows Desktop runtime available

## 3. App Implementation

Converted project to .NET 8 Windows Forms app.

Main changes:

- `CustomTaskManager.csproj`
  - `TargetFramework`: `net8.0-windows`
  - `OutputType`: `WinExe`
  - `UseWindowsForms`: `true`
- `Program.cs`
  - WinForms app entry point
- `MainForm.cs`
  - Main UI and feature wiring

Added models:

- `ProcessSnapshot`
- `StartupEntry`
- `AutomationRule`
- `HistorySummary`
- `PerformanceSnapshot`

Added services:

- `NativeProcess`
- `ProcessMonitor`
- `StartupManager`
- `RuleStore`
- `RuleEngine`
- `HistoryStore`
- `SystemPerformanceMonitor`

Added control:

- `PerformanceGraph`

## 4. Implemented Features

Processes:

- Live process list
- Search by name/PID/path
- CPU percent
- RAM MB
- Threads
- Handles
- Status
- Start time
- Path
- Parent-child process tree
- Kill selected process
- Open process file location

Performance:

- Live CPU graph
- Live memory graph
- Process count
- Thread count
- Handle count
- Uptime
- Logical CPUs
- OS version

Startup:

- Read current-user startup entries
- Read all-users startup entries
- Read 32-bit startup entries
- Disable/enable startup entries
- Store internal registry backup for disabled entries

Rules:

- Process-name condition
- CPU/RAM threshold
- Alert action
- Kill-process action
- Saved to `%AppData%\CustomTaskManager\rules.json`

History:

- Process metric sampling
- Saved to `%AppData%\CustomTaskManager\metrics.csv`
- Summary for last 1 hour, 24 hours, 7 days

## 5. Execution And Bug Fix

User said window was not opening.

Investigation:

- App process exited.
- Windows Event Log showed `.NET Runtime` crash.

Crash:

- `System.InvalidOperationException`
- `SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize`

Fix:

- Removed unsafe splitter setup during constructor time.
- Added safe `ConfigureProcessSplitter(SplitContainer split)` after handle creation/resize.

Final verification:

- `dotnet build`: success
- `0 warnings`
- `0 errors`
- Window title: `Custom Task Manager`
- Fresh crash events: `0`

## 6. Run Instructions

From terminal:

```powershell
cd D:\CustomTaskManager
dotnet run
```

Direct executable:

```powershell
cd D:\CustomTaskManager
.\bin\Debug\net8.0-windows\CustomTaskManager.exe
```

Or:

```powershell
Start-Process "D:\CustomTaskManager\bin\Debug\net8.0-windows\CustomTaskManager.exe"
```

## 7. History System Created

User requested that future work/chat context be saved so another Codex ID can continue.

Created folder:

- `work-history`

Important files:

- `README.md`
- `HANDOFF_FOR_NEXT_CODEX.md`
- `CONVERSATION_LOG.md`
- `UPDATE_PROTOCOL.md`
- `PROJECT_SUMMARY.md`
- `TASK_MANAGER_COMPARISON.md`
- `DIFFERENTIATING_FEATURE_IDEAS.md`
- `NON_OVERLAPPING_CHAT_HISTORY.md`

Important limitation explained:

- Codex cannot automatically log messages in background.
- Codex can update history while actively responding/working.

## 8. Comparison Discussion

User asked how this project differs from inbuilt Windows Task Manager.

Clarified:

- Many basic things overlap:
  - Process list
  - CPU/RAM
  - End task
  - Startup enable/disable
  - Performance view

Strong differences:

- Automation rules
- Process history tracking
- Custom/extensible workflow
- Future reports/alerts/remote dashboard

Correction made:

- Startup disable/enable is not a strong difference because Windows Task Manager already does it.
- Startup backup is currently only a minor/internal difference.

## 9. Feature Ideas Discussion

User asked for features that genuinely differ from inbuilt Task Manager.

Ideas saved:

- Smart alerts
- Startup change monitor
- App usage timeline
- Resource budget per app
- Weekly performance report
- Gaming/Study/Work modes
- Remote dashboard
- Process notes and labels
- Process reputation cache

Important correction:

- Basic suspicious process detection can overlap with Windows Security/Task Manager clues.
- Strong version should be "Suspicious Process Detector With Custom Scoring":
  - path risk
  - usage anomaly
  - startup change link
  - first-seen history
  - user labels
  - explainable score

## 10. Current Best Differentiators

Most interview-safe differences:

1. Automation rules
2. Process history tracking
3. Smart alerts
4. Startup change monitoring, not simple startup disable
5. App usage timeline/reporting
6. Custom profiles/modes

Avoid claiming as unique:

- Simple process list
- Simple CPU/RAM display
- Simple kill process
- Simple startup disable
- Basic suspicious process detection

## 11. Current User Preference

User prefers:

- Hinglish explanation
- Simple practical examples
- Honest correction if a feature is not truly different
- History files updated without duplicate/overlapping content

## 12. Option 1 Remote Dashboard Applied

User asked to apply `opt 1`, meaning mobile as remote dashboard for the Windows laptop.

Implementation status:

- Basic remote dashboard is implemented.
- App starts a local ASP.NET Core server on port `5055`.
- Browser dashboard endpoint: `http://localhost:5055`
- JSON status endpoint: `http://localhost:5055/api/status`
- Same-Wi-Fi phone URL from latest verification: `http://192.168.1.14:5055`

Code pieces:

- `Services/DashboardServer.cs`
- `Services/DashboardState.cs`
- `Models/DashboardSnapshot.cs`
- `Services/BatteryMonitor.cs`
- `Models/BatterySnapshot.cs`
- `MainForm.cs`
- `CustomTaskManager.csproj` with `Microsoft.AspNetCore.App` framework reference

Dashboard shows:

- CPU
- Memory
- Battery
- Power
- Processes
- Threads
- Handles
- Uptime
- Top CPU process table
- Top Memory process table

Fixes/verification:

- Fixed dashboard compile errors around `UseUrls` and `StopAsync`.
- Escaped process names in dashboard HTML table rows.
- `dotnet build` succeeded with `0 warnings` and `0 errors`.
- Updated app launched successfully as PID `20424`.
- Local dashboard page returned HTTP `200`.
- Dashboard API returned live JSON.

## 13. Dashboard Auth And UI Polish

User asked to improve dashboard password/auth and UI polish.

Implemented on 30 May 2026:

- Dashboard now has access-key authentication.
- Access key is generated locally and stored at `%AppData%\CustomTaskManager\dashboard.key`.
- Performance tab shows authenticated dashboard URLs and key text.
- `Open dashboard` opens the local authenticated URL.
- `/api/status` returns HTTP `401` without correct key.
- Correct key can be sent through `X-Dashboard-Key` header or `key` query parameter.
- Browser dashboard has login screen if opened without key.
- Dashboard UI is more polished:
  - sticky header
  - Live/Locked badge
  - metric cards
  - CPU/memory/battery meters
  - cleaner graphs
  - improved mobile responsive layout
  - cleaner Top CPU and Top Memory process tables

Verification:

- `dotnet build`: success, `0 warnings`, `0 errors`.
- App relaunched as PID `12760`.
- App responding: `True`.
- Dashboard page: HTTP `200`.
- API without key: HTTP `401`.
- API with key: HTTP `200`.
- Wi-Fi API with key: HTTP `200`.

## 14. Visualization Tab With Pie Chart

User asked for a separate pie chart near History with a `Visualization` view for overall system analysis using CPU, GPU, battery, and useful extra signals.

Implemented:

- New `Visualization` tab next to `History`.
- Custom donut/pie chart control:
  - `Controls/SystemPieChart.cs`
  - `Controls/PieChartSlice.cs`
- GPU support:
  - `Models/GpuSnapshot.cs`
  - `Services/GpuMonitor.cs`
- `PerformanceSnapshot` now includes `Gpu`.
- `SystemPerformanceMonitor` captures GPU via Windows `GPU Engine` performance counters.
- Visualization combines CPU, GPU, memory, battery risk, and process/thread/handle pressure.
- UI shows overall pressure score, status, metric cards, and recommendation text.
- GPU unavailable state is handled safely.

Verification:

- `dotnet build`: success, `0 warnings`, `0 errors`.
- App relaunched as PID `15500`.
- App responding: `True`.
- Dashboard API JSON included `performance.gpu`.
- GPU counter was available during verification.
