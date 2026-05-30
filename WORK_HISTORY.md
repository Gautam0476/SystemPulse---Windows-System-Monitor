# Custom Task Manager - Work History

Date: 28-29 May 2026
Project path: `D:\CustomTaskManager`

## Original Goal

Windows Task Manager jaisa ek custom desktop utility banana tha jo system ke running processes, RAM/CPU usage, process tree, process killing, startup apps, automation rules aur history tracking dikha sake.

## Initial State

Project initially ek basic .NET console template tha:

- `Program.cs` mein sirf `Hello, World!`
- `CustomTaskManager.csproj` normal console app tha
- Koi UI, services, models ya process monitoring logic nahi tha

## Project Conversion

Console project ko Windows Forms desktop app mein convert kiya:

- `TargetFramework` ko `net8.0-windows` kiya
- `OutputType` ko `WinExe` kiya
- `UseWindowsForms` enable kiya
- `Program.cs` ko WinForms entry point banaya

Main app entry:

- `ApplicationConfiguration.Initialize()`
- `Application.Run(new MainForm())`

## Features Implemented

### 1. Live Process Monitor

Implemented in:

- `Services/ProcessMonitor.cs`
- `Services/NativeProcess.cs`
- `Models/ProcessSnapshot.cs`

Features:

- Running processes ki live list
- PID
- Parent PID
- Process name
- CPU percentage
- RAM usage in MB
- Thread count
- Handle count
- Start time
- Executable path
- Responsive / not responding status

CPU percentage ko process CPU delta aur elapsed time ke basis par calculate kiya gaya.

### 2. Process Tree

Implemented parent-child process mapping using Windows Toolhelp API:

- `CreateToolhelp32Snapshot`
- `Process32First`
- `Process32Next`

UI mein left side process tree add kiya gaya.

### 3. Process Killer

Implemented in:

- `MainForm.cs`

Features:

- Selected process ko terminate karna
- Child process tree ke saath kill karna
- Self-kill protection
- Confirmation dialog before killing

Uses:

- `Process.Kill(entireProcessTree: true)`

### 4. Open Process Location

Selected process ka executable path Explorer mein open karne ka option add kiya.

Uses:

- `explorer.exe /select,"path"`

### 5. Startup Manager

Implemented in:

- `Services/StartupManager.cs`
- `Models/StartupEntry.cs`

Features:

- Current user startup apps read karna
- All users startup apps read karna
- 32-bit startup registry view read karna
- Startup entry disable karna
- Disabled entry ka backup registry mein save karna
- Disabled entry ko restore/enable karna

Registry paths used:

- `Software\Microsoft\Windows\CurrentVersion\Run`
- `Software\CustomTaskManager\DisabledStartup`

Note:

- HKLM/all-users startup entries change karne ke liye admin permission lag sakti hai.

### 6. Automation Rules

Implemented in:

- `Models/AutomationRule.cs`
- `Services/RuleEngine.cs`
- `Services/RuleStore.cs`

Features:

- Rule active/inactive toggle
- Process name contains condition
- Metric selection:
  - CPU percent
  - Memory MB
- Threshold value
- Action selection:
  - Alert
  - Kill process
- Rule cooldown to avoid repeated triggers
- Rules persist in JSON

Rules save path:

- `%AppData%\CustomTaskManager\rules.json`

### 7. History Tracking

Implemented in:

- `Services/HistoryStore.cs`
- `Models/HistorySummary.cs`

Features:

- Process metrics CSV logging
- Top processes ka periodic sample
- History summary views:
  - Last 1 hour
  - Last 24 hours
  - Last 7 days
- Average CPU
- Average RAM
- Max RAM
- Sample count

History save path:

- `%AppData%\CustomTaskManager\metrics.csv`

### 8. Main Windows Forms UI

Implemented in:

- `MainForm.cs`

Tabs added:

- Processes
- Startup
- Rules
- History

UI features:

- Dark desktop dashboard
- Search box for process name, PID, or path
- Process tree panel
- Process data grid
- Startup app grid
- Rules editor
- History summary grid
- Status bar
- Manual refresh buttons

### 9. Performance Tab

Implemented in:

- `Models/PerformanceSnapshot.cs`
- `Services/SystemPerformanceMonitor.cs`
- `Controls/PerformanceGraph.cs`
- `MainForm.cs`

Features:

- Task Manager style `Performance` tab
- Live CPU usage graph
- Live memory usage graph
- CPU percentage from Windows `GetSystemTimes`
- Memory usage from Windows `GlobalMemoryStatusEx`
- Battery percentage/status from Windows power status
- Power line status
- Process count
- Thread count
- Handle count
- System uptime
- Logical CPU count
- Windows version

Refresh behavior:

- Performance data updates with the same 2 second refresh timer used by the process monitor.

### 10. Mobile / Browser Remote Dashboard

Implemented in:

- `Services/DashboardServer.cs`
- `Services/DashboardState.cs`
- `Models/DashboardSnapshot.cs`
- `Services/BatteryMonitor.cs`
- `Models/BatterySnapshot.cs`
- `MainForm.cs`

Features:

- Local ASP.NET Core dashboard server on port `5055`
- Browser UI at `http://localhost:5055`
- JSON API at `http://localhost:5055/api/status`
- Same-Wi-Fi mobile access using laptop IP, example `http://192.168.1.14:5055`
- Access-key authentication for live dashboard API data
- Login screen for browser/mobile dashboard when key is missing
- Live CPU and memory graphs in the browser
- Battery, power, process, thread, handle, and uptime cards
- Top CPU and Top Memory process tables

Fixes applied while enabling:

- Added `Microsoft.AspNetCore.Hosting` import so `UseUrls` compiles.
- Updated dashboard shutdown to use a cancellation token with a 2 second timeout.
- Escaped process names before rendering dashboard table rows.
- Added persistent dashboard access key at `%AppData%\CustomTaskManager\dashboard.key`.
- Protected `/api/status` with the `X-Dashboard-Key` header or `key` query value.
- Polished dashboard UI with login screen, status badge, metric cards, meters, cleaner tables, and responsive layout.

### 11. Visualization Tab With Pie Chart

Implemented in:

- `Controls/SystemPieChart.cs`
- `Controls/PieChartSlice.cs`
- `Models/GpuSnapshot.cs`
- `Services/GpuMonitor.cs`
- `Models/PerformanceSnapshot.cs`
- `Services/SystemPerformanceMonitor.cs`
- `MainForm.cs`

Features:

- New `Visualization` tab placed next to `History`
- Custom donut/pie chart for overall system pressure analysis
- Overall pressure score with status:
  - Healthy
  - Light pressure
  - Moderate pressure
  - High pressure
  - Critical pressure
- Analysis inputs:
  - CPU usage
  - GPU usage from Windows `GPU Engine` performance counters
  - Memory usage
  - Battery risk
  - Process/thread/handle pressure
- Recommendation text based on the highest pressure area
- Graceful GPU fallback: if GPU counters are unavailable, the app shows `GPU: Unavailable` instead of crashing

## Bug Found During Execution

Initial execution mein window open nahi ho rahi thi.

Root cause:

- App crash kar rahi thi due to `SplitContainer`.
- Error:
  - `SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize.`

Event Log se exact crash trace nikala gaya:

- Provider: `.NET Runtime`
- Exception: `System.InvalidOperationException`
- File: `MainForm.cs`
- Problem area: `BuildProcessesTab()`

## Bug Fix

Fix implemented in:

- `MainForm.cs`

Changes:

- Constructor time par fixed `SplitterDistance` set karna remove kiya
- `Panel1MinSize` / `Panel2MinSize` dependency remove ki
- Splitter distance ko control resize/handle creation ke baad safe calculation ke saath set kiya

Safe method added:

- `ConfigureProcessSplitter(SplitContainer split)`

## Verification Commands Run

Build command:

```powershell
dotnet build
```

Result:

- Build succeeded
- 0 warnings
- 0 errors

Execution command:

```powershell
Start-Process -FilePath ".\bin\Debug\net8.0-windows\CustomTaskManager.exe" -PassThru
```

Final execution result:

- App running
- PID: `3520`
- Responding: `True`
- Main window title: `Custom Task Manager`
- Main window handle found
- Fresh crash events: `0`
- Foreground activation successful

## Files Added

- `MainForm.cs`
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
- `Controls/PerformanceGraph.cs`
- `WORK_HISTORY.md`

## Files Modified

- `CustomTaskManager.csproj`
- `Program.cs`
- `MainForm.cs`

## How To Run

From project folder:

```powershell
cd D:\CustomTaskManager
dotnet run
```

Or directly run:

```powershell
D:\CustomTaskManager\bin\Debug\net8.0-windows\CustomTaskManager.exe
```

## Current Status

Custom Task Manager app is built and executable. It has live process monitoring, process tree, process killer, startup manager, automation rules, process history tracking, performance graphs, battery/power metrics, and a mobile/browser dashboard.

Latest remote dashboard verification on 29 May 2026:

- `dotnet build`: succeeded with 0 warnings and 0 errors
- Running app PID: `20424`
- App responding: `True`
- Dashboard page `http://localhost:5055/`: HTTP 200
- Dashboard API `http://localhost:5055/api/status`: returned live JSON
- Wi-Fi dashboard API `http://192.168.1.14:5055/api/status`: HTTP 200 from the laptop

Latest secure dashboard verification on 30 May 2026:

- `dotnet build`: succeeded with 0 warnings and 0 errors
- Running app PID: `12760`
- App responding: `True`
- Dashboard page `http://localhost:5055/`: HTTP 200
- Dashboard API without key: HTTP 401
- Dashboard API with correct key: HTTP 200
- Wi-Fi dashboard API with correct key at `http://192.168.1.14:5055/api/status`: HTTP 200

Latest visualization verification on 30 May 2026:

- `dotnet build`: succeeded with 0 warnings and 0 errors
- Running app PID: `15500`
- App responding: `True`
- Dashboard API returned `performance.gpu`
- GPU monitor returned:
  - `isAvailable`: `true`
  - `usagePercent`: `0`
  - source: `Windows GPU Engine counters`

## Ongoing History / Handoff Folder

User requested that future searches, responses, changes, commands, and project context should be preserved so another Codex ID can continue after context/session limit.

Created structured folder:

- `work-history/README.md`
- `work-history/HANDOFF_FOR_NEXT_CODEX.md`
- `work-history/CONVERSATION_LOG.md`
- `work-history/UPDATE_PROTOCOL.md`

Primary handoff file for a new Codex:

- `work-history/HANDOFF_FOR_NEXT_CODEX.md`

Important limitation:

- Codex cannot automatically log future conversation in the background.
- While actively responding or working, Codex should keep this folder updated after meaningful changes.
