# Custom Task Manager - Easy Project Explanation Guide

This guide is written so you can explain the project easily to a teacher, interviewer, friend, or in a viva.

## 1. One Line Summary

Custom Task Manager is a .NET Windows desktop app that monitors running processes, CPU/RAM/GPU performance, startup apps, automation rules, usage history, alerts, visualization, and a secured local dashboard.

In simple words:

> Ye Windows Task Manager ka custom advanced version hai. Isme normal process monitoring ke saath automation, history, smart alerts, Smart RAM Advisor, startup control, performance graphs, and mobile/browser dashboard bhi hai.

## 2. Problem Statement

Normal users usually open Windows Task Manager only after the system becomes slow. They manually check which app is using more CPU or RAM and then decide what to close.

This project solves that by giving:

- Live process monitoring.
- Easy search by process name, PID, or path.
- Parent-child process tree.
- Safe manual process kill.
- Smart RAM suggestions.
- Automation rules for CPU/RAM thresholds.
- Desktop alerts.
- History tracking.
- Performance and visualization dashboards.
- Startup app management.
- Local browser/mobile dashboard.

## 3. Tech Stack

- Language: C#
- Framework: .NET 8
- UI: Windows Forms
- Platform: Windows
- Storage:
  - JSON for automation rules
  - CSV for process history
  - Registry backup for disabled startup entries
- OS APIs:
  - Windows process APIs
  - Performance counters
  - Registry access
  - NotifyIcon desktop notifications
- Local dashboard:
  - ASP.NET Core server running inside the desktop app

## 4. Main Features

### Processes Tab

This is the main screen of the app.

It shows:

- Running process list.
- PID of each process.
- Parent process ID.
- CPU usage.
- RAM usage.
- Thread count.
- Handle count.
- Process status.
- Start time.
- Executable path.

Important actions:

- Search: find process by name, PID, or path.
- Refresh: manually update process data.
- Open file: open selected process executable location.
- Kill: terminate selected process after confirmation.
- Theme toggle: switch between dark and light theme.

### Process Tree

The left side shows process hierarchy.

Example:

```text
powershell
  dotnet
    CustomTaskManager
```

This helps explain which process started another process.

### Smart RAM Advisor

This is a custom feature added to make the project unique.

It checks running processes and suggests which app can be reviewed for closing to free RAM.

It does not blindly kill anything. It only selects a suggestion so the user can review it.

It avoids system-critical processes like:

- System
- csrss
- winlogon
- services
- lsass
- svchost
- explorer

Example:

```text
Smart RAM Advisor: review chrome
chrome ~578 MB | Code ~518 MB | Microsoft.CodeAnalysis.LanguageServer ~375 MB
Why: 578 MB RAM, 17.7% CPU, looks like a user app. Save work before closing.
```

How to explain it:

> Ye feature local smart scoring use karta hai. High RAM, high CPU, not responding status, and user-app paths ko score karta hai. System processes ko avoid karta hai. Isliye ye fake AI nahi hai, but explainable smart recommendation system hai.

### Performance Tab

This tab shows system-level performance.

It includes:

- CPU graph.
- Memory graph.
- GPU graph.
- CPU percentage.
- Memory usage in GB.
- GPU status.
- Battery status.
- Power status.
- Process count.
- Thread count.
- Handle count.
- Uptime.
- Logical CPU count.
- OS information.

### Startup Tab

This tab manages startup apps.

It reads Windows registry startup locations:

- Current user startup entries.
- All users startup entries.
- 32-bit startup entries.

It can:

- Show startup apps.
- Disable startup app.
- Enable startup app again.
- Store backup of disabled entry.

How to explain it:

> Jab Windows start hota hai, kuch apps automatically start hoti hain. Ye tab un registry entries ko read karta hai and user ko enable/disable karne deta hai.

### Rules Tab

This is the automation feature.

User can create rules like:

```text
If chrome RAM > 3000 MB, then Alert
If some process CPU > 80%, then Kill
```

Rule contains:

- Process name condition.
- Metric: CPU percent or RAM MB.
- Threshold value.
- Action: Alert or Kill.
- Enabled/Disabled state.

Why it is important:

> Windows Task Manager me user manually check karta hai. Is app me rule bana sakte ho, so app automatically alert ya action le sakta hai.

### History Tab

This app stores process metrics in CSV.

It can show summaries for:

- Last 1 hour.
- Last 24 hours.
- Last 7 days.

This is useful because normal monitoring only shows current state, but history helps understand past behavior.

### Visualization Tab

This tab converts system health into a visual pressure score.

It uses:

- CPU pressure.
- GPU pressure.
- Memory pressure.
- Battery pressure.
- Process count pressure.

It displays:

- Pie/donut chart.
- Overall pressure score.
- Status message.
- Recommendation.

Example:

```text
Moderate pressure
Memory is high; review background apps.
```

### Local Browser/Mobile Dashboard

The app runs a local secured dashboard on:

```text
http://localhost:5055
```

It also supports same-Wi-Fi access using the laptop IP.

Security:

- API endpoint is protected by an access key.
- Key is stored in AppData.

How to explain it:

> Desktop app ke andar ek lightweight local server run hota hai. Isse browser ya mobile se system status dekh sakte hain, but access key ke bina API data nahi milega.

### Smart Alerts

The app gives desktop notifications using WinForms NotifyIcon.

Alerts are triggered when:

- Automation rule matches.
- CPU stays above 85 percent for several refreshes.
- Memory stays above 85 percent for several refreshes.

This makes the app proactive instead of only passive.

## 5. How The App Works Internally

Simple flow:

```text
Program.cs starts MainForm
MainForm builds all tabs and UI
Refresh timer runs every few seconds
ProcessMonitor captures process list
NativeProcess maps parent-child processes
SystemPerformanceMonitor captures CPU/RAM/GPU/battery/system data
MainForm updates grids, graphs, advisor, alerts, history, and dashboard state
DashboardServer serves latest status to browser/mobile
```

## 6. Important Files

| File | Purpose |
|---|---|
| `Program.cs` | App entry point |
| `MainForm.cs` | Main UI, tabs, refresh loop, button actions |
| `Models/ProcessSnapshot.cs` | Data model for each process |
| `Services/ProcessMonitor.cs` | Captures running processes and CPU/RAM data |
| `Services/NativeProcess.cs` | Reads parent-child process relationship |
| `Services/ProcessAdvisor.cs` | Smart RAM Advisor logic |
| `Services/SystemPerformanceMonitor.cs` | CPU/RAM/system metrics |
| `Services/GpuMonitor.cs` | GPU usage through Windows counters |
| `Services/StartupManager.cs` | Startup app registry enable/disable |
| `Services/RuleEngine.cs` | Automation rules logic |
| `Services/HistoryStore.cs` | CSV history storage |
| `Services/DashboardServer.cs` | Local secured browser/mobile dashboard |
| `Controls/PerformanceGraph.cs` | Custom graph control |
| `Controls/SystemPieChart.cs` | Visualization chart control |

## 7. Difference Between Windows Task Manager And This Project

| Area | Windows Task Manager | Custom Task Manager |
|---|---|---|
| Main purpose | Built-in real-time system monitor | Custom extendable process management tool |
| Process list | Yes | Yes |
| CPU/RAM per process | Yes | Yes |
| End process | Yes | Yes, with confirmation and child-process tree kill |
| Search | Basic | Search by name, PID, and path |
| Process tree | Limited/detail view style | Clear parent-child tree on main process screen |
| Startup apps | Enable/disable | Enable/disable plus backup of disabled entries |
| Automation rules | No | Yes, CPU/RAM threshold rules with alert/kill action |
| Smart RAM suggestion | No | Yes, Smart RAM Advisor ranks safer high-memory user apps |
| Desktop alerts | Limited | Custom alerts for rules and sustained high CPU/RAM |
| History tracking | Limited/not project-focused | CSV history with 1h/24h/7d summaries |
| Visualization | Standard graphs | Custom pressure score using CPU/GPU/RAM/battery/process load |
| Browser/mobile dashboard | No | Yes, secured local dashboard on port 5055 |
| Customization | Closed built-in tool | Fully editable codebase |
| Learning value | User tool | Demonstrates C#, WinForms, OS APIs, registry, counters, local server |

## 8. Honest Difference Statement

Do not say:

```text
My app is better than Windows Task Manager in every way.
```

Say this:

```text
Windows Task Manager is a polished built-in tool. My Custom Task Manager is a learning and extension-focused tool that adds automation rules, Smart RAM Advisor, history tracking, smart alerts, startup backup, and a secured local dashboard.
```

## 9. 60 Second Explanation Script

> This is my Custom Task Manager built using C# and .NET 8 WinForms. It monitors live Windows processes and shows CPU, RAM, PID, parent process, threads, handles, status, start time, and path. I also added a process tree to show parent-child relationships. Apart from normal Task Manager features like refresh, kill, open file location, and startup management, my project adds automation rules, smart desktop alerts, process history, visualization, GPU performance, and a secured browser/mobile dashboard. A unique feature is Smart RAM Advisor, which suggests high-memory user apps that can be reviewed before closing, while avoiding critical Windows processes. The goal was to build an OS-level monitoring project, not just a basic CRUD app.

## 10. 2 Minute Explanation Script

> My project is a Custom Task Manager for Windows. It is built with C# and .NET 8 WinForms. The app refreshes process and system data every few seconds. In the Processes tab, it shows a live table of running processes with PID, parent PID, CPU usage, memory usage, thread count, handle count, status, start time, and executable path. On the left side, it shows a parent-child process tree, which helps understand which process launched another process.
>
> The project also includes a Smart RAM Advisor. It analyzes running processes and suggests safer high-memory user apps to review for closing. It avoids important Windows system processes and gives a reason for every suggestion.
>
> The Performance tab shows CPU, memory, and GPU graphs along with system cards like battery, uptime, OS, process count, threads, and handles. The Startup tab reads registry startup entries and allows enabling or disabling them with backup. The Rules tab lets the user create automation rules like alert me if chrome uses more than 3000 MB RAM, or kill a process if it crosses a CPU threshold. The History tab stores process metrics in CSV and shows summaries. The Visualization tab gives an overall system pressure score using CPU, GPU, RAM, battery, and process load. The app also runs a secured local dashboard on port 5055, so system status can be viewed from a browser or mobile on the same Wi-Fi.
>
> Compared to Windows Task Manager, this project is not trying to replace the built-in tool. It adds custom features like automation, smart recommendations, history, alerts, visualization, and a local dashboard to demonstrate OS-level programming.

## 11. Demo Flow

Use this order while showing the project:

1. Open the app and show live process count, CPU, RAM, and search.
2. Search for `chrome`, `code`, or any running app.
3. Select a process and show its details in the table.
4. Show the process tree on the left.
5. Explain Smart RAM Advisor at the top.
6. Open Performance tab and show CPU/RAM/GPU graphs.
7. Open Startup tab and explain registry startup entries.
8. Open Rules tab and create a sample alert rule.
9. Open History tab and explain CSV summaries.
10. Open Visualization tab and explain system pressure score.
11. Mention the browser/mobile dashboard and access key.

## 12. Strong Points To Mention

- It uses real Windows process data.
- It calculates CPU percentage by comparing process CPU time between refreshes.
- It uses native Windows APIs for parent process mapping.
- It reads registry startup entries.
- It stores history in CSV.
- It uses JSON for rules.
- It has custom WinForms controls for graphs and charts.
- It has smart notifications.
- It has a secured local dashboard.
- It has explainable Smart RAM Advisor.

## 13. Limitations To Mention Honestly

- It needs Windows because it uses WinForms and Windows APIs.
- Some protected system process details may not be readable without permission.
- GPU availability depends on Windows GPU performance counters.
- Killing a process can close unsaved work, so confirmation is required.
- It is a learning/custom tool, not a full replacement for Microsoft Task Manager.

## 14. Future Improvements

- Suspicious process scoring.
- Per-process GPU usage.
- Exportable PDF/HTML reports from history.
- Startup change monitor.
- Profiles like Gaming Mode, Study Mode, Coding Mode.
- Optional real ML model trained on usage history.
- Better mobile dashboard UI.
- Process grouping by app family.

## 15. Best Final Line

> This project shows that I understand not only UI development, but also Windows process monitoring, registry operations, performance counters, background refresh, local storage, notifications, and local dashboard hosting in .NET.

