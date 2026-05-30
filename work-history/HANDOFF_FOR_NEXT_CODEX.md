# Handoff For Next Codex

Project: `D:\CustomTaskManager`
Current date in session: 30 May 2026
Tech stack: .NET 8 Windows Forms desktop app

## User Goal

User ek custom Windows Task Manager / process monitor bana raha hai. App ko resume/project-worthy banana hai, normal Task Manager ki copy nahi. Features should show OS-level capability:

- Live process monitoring
- Process tree
- CPU/RAM metrics
- Process killer
- Startup manager
- Automation rules
- History tracking
- Performance dashboard like Windows Task Manager
- Mobile/browser remote dashboard for laptop stats
- Visualization tab with overall system pressure pie chart

User Hinglish mein baat karta hai. Tone simple, direct, helpful rakho.

## Current Project State

The app is a Windows Forms app and builds successfully.

Run from:

```powershell
cd D:\CustomTaskManager
dotnet run
```

Or:

```powershell
D:\CustomTaskManager\bin\Debug\net8.0-windows\CustomTaskManager.exe
```

Last verified:

- `dotnet build` succeeded
- `0 warnings`
- `0 errors`
- Window title: `Custom Task Manager`
- Fresh crash events: `0`
- Mobile dashboard API responded at `http://localhost:5055/api/status`
- Wi-Fi dashboard API responded locally at `http://192.168.1.14:5055/api/status`
- Dashboard page responded at `http://localhost:5055/`
- Unauthenticated dashboard API returned HTTP `401`
- Authenticated dashboard API returned HTTP `200`
- Visualization build/runtime verified; app responding as PID `15500`
- GPU data appeared in dashboard JSON through `performance.gpu`

## Implemented Features

### Processes Tab

- Live process list
- Search by process name, PID, or path
- Process tree with parent-child process relation
- CPU percent
- RAM MB
- Threads
- Handles
- Status
- Start time
- Path
- Kill selected process
- Open process location

### Performance Tab

- Live CPU graph
- Live Memory graph
- Battery percentage/status
- Power line status
- Process count
- Thread count
- Handle count
- System uptime
- Logical CPU count
- OS version
- Mobile dashboard URL bar
- Open dashboard button

### Mobile / Browser Dashboard

- Starts local ASP.NET Core dashboard server on port `5055`
- Binds to `0.0.0.0` so another device on the same Wi-Fi can open it
- Uses access-key authentication for `/api/status`
- Access key is shown in the Performance tab URL/key text
- Access key is stored locally at `%AppData%\CustomTaskManager\dashboard.key`
- Shows CPU, memory, battery, power, processes, threads, handles, uptime
- Shows Top CPU and Top Memory process tables
- JSON endpoint: `http://localhost:5055/api/status`
- Browser UI endpoint: `http://localhost:5055/`
- Current Wi-Fi URL from latest verification: `http://192.168.1.14:5055`

### Startup Tab

- Reads startup apps from registry
- Current-user startup entries
- All-users startup entries
- 32-bit registry startup entries
- Disable startup entry
- Restore/enable disabled startup entry

Admin may be required for HKLM/all-users entries.

### Rules Tab

- Automation rules
- Process name contains condition
- CPU or Memory threshold
- Alert action
- Kill process action
- Rules stored in `%AppData%\CustomTaskManager\rules.json`

### History Tab

- Periodic process metric samples
- CSV stored in `%AppData%\CustomTaskManager\metrics.csv`
- Shows summaries for:
  - Last 1 hour
  - Last 24 hours
  - Last 7 days

### Visualization Tab

- Added next to `History`
- Shows custom donut/pie chart for overall system pressure
- Combines CPU, GPU, memory, battery risk, and process/thread/handle pressure
- Shows overall score and status text
- Shows recommendation based on highest pressure area
- GPU data uses Windows `GPU Engine` performance counters
- If GPU counters are unavailable, UI shows GPU unavailable and continues safely

## Important Files

- `Program.cs`: WinForms app entry
- `CustomTaskManager.csproj`: .NET 8 Windows Forms project config
- `MainForm.cs`: Main UI and event wiring
- `Controls/PerformanceGraph.cs`: Custom graph control
- `Models/ProcessSnapshot.cs`
- `Models/StartupEntry.cs`
- `Models/AutomationRule.cs`
- `Models/HistorySummary.cs`
- `Models/PerformanceSnapshot.cs`
- `Models/BatterySnapshot.cs`
- `Models/DashboardSnapshot.cs`
- `Models/GpuSnapshot.cs`
- `Controls/PieChartSlice.cs`
- `Controls/SystemPieChart.cs`
- `Services/NativeProcess.cs`
- `Services/ProcessMonitor.cs`
- `Services/StartupManager.cs`
- `Services/RuleStore.cs`
- `Services/RuleEngine.cs`
- `Services/HistoryStore.cs`
- `Services/SystemPerformanceMonitor.cs`
- `Services/BatteryMonitor.cs`
- `Services/GpuMonitor.cs`
- `Services/DashboardState.cs`
- `Services/DashboardServer.cs`
- `WORK_HISTORY.md`: Original broad work history
- `work-history/`: structured handoff folder

## Known Past Bug

Window initially did not open because the app crashed at startup.

Root cause:

- `SplitContainer` was configured before it had a valid width.
- Exception: `SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize.`

Fix:

- Removed fixed splitter setup during construction.
- Added safe `ConfigureProcessSplitter(SplitContainer split)` after handle creation/resize.

## User Request For History

User asked that future searches/responses/work should be saved in this history folder so another Codex account can understand everything after context limit/session change.

Best effort behavior:

- Keep this folder updated after meaningful work.
- Save important commands, results, decisions, features, and bugs.
- Keep writing clear handoff notes for next Codex.

Do not claim automatic background logging. Codex can update files only while actively responding/working.
