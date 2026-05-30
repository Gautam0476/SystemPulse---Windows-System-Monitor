# Custom Task Manager Project Summary

## One-Line Summary

Custom Task Manager ek .NET 8 Windows Forms desktop app hai jo Windows ke running processes, system performance, GPU usage, battery/power status, startup apps, automation rules, process history, visualization dashboard aur mobile/browser dashboard ko monitor/control karta hai.

## How The App Works

App ka entry point `Program.cs` hai. Wahan se `MainForm` launch hota hai.

Main UI `MainForm.cs` mein hai. App ek refresh timer use karta hai jo roughly har 2 seconds mein process aur performance data update karta hai.

Core flow:

1. `ProcessMonitor` system ke running processes read karta hai using `System.Diagnostics.Process.GetProcesses()`.
2. Har process ka PID, name, CPU, RAM, threads, handles, status, start time aur path collect hota hai.
3. `NativeProcess` Windows Toolhelp API use karke parent-child process relation nikalta hai, jisse process tree banta hai.
4. `SystemPerformanceMonitor` Windows APIs use karta hai:
   - `GetSystemTimes` for CPU usage
   - `GlobalMemoryStatusEx` for RAM usage
5. `BatteryMonitor` Windows power status read karta hai using `SystemInformation.PowerStatus`.
6. `GpuMonitor` Windows `GPU Engine` performance counters se GPU utilization read karta hai.
7. `StartupManager` Windows Registry ke startup entries read/modify karta hai.
8. `RuleEngine` user-defined automation rules evaluate karta hai.
9. `HistoryStore` process metrics ko CSV file mein save karke later summary show karta hai.
10. `SystemPieChart` overall system pressure pie chart draw karta hai.
11. `DashboardState` latest performance/process snapshot hold karta hai.
12. `DashboardServer` local ASP.NET Core web server start karta hai for browser/mobile dashboard.

## Main Features

### Processes Tab

- Live running process list
- Search by name, PID, or executable path
- Process tree
- CPU percentage
- RAM usage
- Threads
- Handles
- Responsive / not responding status
- Process start time
- Executable path
- Kill selected process with child process tree
- Open process location in File Explorer

### Performance Tab

- Task Manager style live CPU graph
- Live memory graph
- Battery percentage/status
- Power line status
- Process count
- Thread count
- Handle count
- System uptime
- Logical CPU count
- Windows version
- Dashboard URL shown in the app
- Open dashboard button

### Mobile / Browser Dashboard

- Local dashboard server runs on port `5055`
- Laptop browser URL: `http://localhost:5055`
- Phone URL on same Wi-Fi: laptop IP plus port, example `http://192.168.1.14:5055`
- Dashboard requires an access key before `/api/status` returns live data
- Access key is shown in the Windows app's Performance tab
- Access key is stored locally at `%AppData%\CustomTaskManager\dashboard.key`
- Dashboard shows live CPU, memory, battery, power, process/thread/handle counts, uptime
- Dashboard shows Top CPU and Top Memory process tables
- API endpoint: `http://localhost:5055/api/status`

### Startup Tab

- Startup apps list
- Current-user startup entries
- All-users startup entries
- 32-bit registry startup entries
- Enable/disable startup entries
- Disabled entries ka backup maintain hota hai

### Rules Tab

- Custom automation rules
- Process name condition
- CPU/RAM threshold
- Alert action
- Kill process action
- Rules saved in JSON

Example:

- If Chrome uses more than 3000 MB RAM, alert or kill it.
- If any process crosses a CPU limit, trigger action.

### History Tab

- Process metrics save hote hain
- Last 1 hour / 24 hours / 7 days summary
- Average CPU
- Average RAM
- Max RAM
- Sample count

### Visualization Tab

- History ke paas separate `Visualization` tab
- Pie/donut chart for overall system pressure
- Inputs:
  - CPU usage
  - GPU usage
  - Memory usage
  - Battery risk
  - Process/thread/handle load
- Overall pressure score and status
- Recommendation text for next action
- GPU unavailable hone par app safely fallback karta hai

## Stored Data

Rules:

- `%AppData%\CustomTaskManager\rules.json`

History:

- `%AppData%\CustomTaskManager\metrics.csv`

Disabled startup backups:

- `HKCU\Software\CustomTaskManager\DisabledStartup`

Dashboard access key:

- `%AppData%\CustomTaskManager\dashboard.key`

## Difference From Inbuilt Windows Task Manager

Windows Task Manager mainly current system state dikhata hai. This custom app extends the idea with user-controlled behavior and project-level customization.

Key differences:

1. Automation rules
   - Inbuilt Task Manager manually process end karta hai.
   - This app threshold-based rules laga sakta hai, like high RAM/CPU par alert or kill.

2. History tracking
   - Task Manager mostly live/current data dikhata hai.
   - This app process data CSV mein save karta hai and later summaries show karta hai.

3. Custom startup backup
   - Task Manager startup enable/disable deta hai.
   - This app registry backup maintain karta hai, so disabled entries restore ho sakti hain.

4. Extendable project
   - Inbuilt Task Manager fixed product hai.
   - This app ka code open/custom hai, so remote monitoring, web dashboard, notifications, SQLite, reports, charts, etc. add ho sakte hain.

5. Mobile/browser dashboard
   - Task Manager mainly same-machine UI hai.
   - This app laptop ke stats ko same Wi-Fi par phone/browser me show kar sakta hai.

6. OS-level learning value
   - Project demonstrates process handling, registry access, P/Invoke, Windows APIs, timers, custom UI, file persistence, and automation.

## Resume Impact

Ye project simple CRUD app se zyada strong dikhta hai because it interacts directly with OS-level concepts:

- Processes
- CPU/RAM metrics
- Windows Registry
- Native Windows APIs
- Battery/power status
- GPU performance counters
- Custom pie chart visualization
- Local ASP.NET Core web server
- JSON dashboard API
- Same-Wi-Fi mobile dashboard
- Process termination
- Persistent monitoring
- Desktop UI
- Automation rules

Interviewer ko clear signal milta hai ki developer sirf web forms nahi, system-level programming bhi samajhta hai.
