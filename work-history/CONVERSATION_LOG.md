# Conversation Log

Compact log of important project decisions, changes, and verification. This file intentionally avoids duplicate long history; use `HANDOFF_FOR_NEXT_CODEX.md` and `PROJECT_SUMMARY.md` for current state.

## Original Project

User wanted a custom Windows Task Manager/process monitor that looks stronger than a simple CRUD project and demonstrates OS-level Windows/.NET knowledge.

Initial project was a basic .NET console app. It was converted to a .NET 8 WinForms app.

## Major Milestones

- Built live process monitor with search, CPU/RAM, status, threads, handles, start time, and path.
- Added parent-child process tree through native Windows process APIs.
- Added selected-process kill action with confirmation and self-kill protection.
- Added open-process-location action.
- Added startup manager for registry startup entries with disable/enable support.
- Added automation rules with CPU/RAM thresholds and alert/kill actions.
- Added CSV history tracking and summary windows.
- Added Performance tab with CPU/memory graphs and system metric cards.
- Added secured local browser/mobile dashboard on port `5055`.
- Added Visualization tab with CPU/GPU/memory/battery/process pressure chart.
- Added GPU graph/card to the Performance tab.
- Refactored repeated UI code in `MainForm.cs` to make files more concise.
- Added visual polish, async process refresh, process-tree refresh optimization, light/dark theme toggle, and smart desktop notifications.
- Condensed work-history docs to reduce repeated notes.

## Recent User Questions Answered

- `Kill` button: forcefully terminates the selected process and child process tree after confirmation.
- `Yes` on kill confirmation: selected process can close immediately; unsaved work can be lost.
- `Refresh`: refreshes process list, CPU/RAM, performance data, automation checks, history sample, and GPU-backed visualization data.
- `Open location`: opens File Explorer and selects the selected process executable.
- GPU in Performance tab: was missing from the UI even though GPU data was captured; now added.

## Latest Code Changes

- `MainForm.cs`
  - Added GPU graph and GPU metric card to Performance tab.
  - Replaced repeated graph setup with `CreatePerformanceGraph`.
  - Replaced repeated equal-grid setup with `CreateEvenGrid`.
  - Replaced repeated metric-card insertion with `AddMetricCard`.
  - Replaced repeated grid-column calls with `AddTextColumns`.
  - Added owner-drawn tab styling, light/dark theme support, and async process refresh.
  - Added WinForms `NotifyIcon` notifications for automation alerts and sustained high CPU/RAM smart alerts.
  - Converted the theme toggle into a reference-style animated pill; the knob slides between moon and sun positions, labels crossfade, and the sun rotates during theme changes.
  - Added a local Smart RAM Advisor that ranks safer high-memory app processes, explains why they are suggested, and selects the top suggestion for manual review.

- `Controls/PerformanceGraph.cs`
  - Added `ValueTextOverride` so GPU can display `Unavailable` when counters are not readable.

- Docs
  - Condensed `WORK_HISTORY.md`, `HANDOFF_FOR_NEXT_CODEX.md`, `PROJECT_SUMMARY.md`, and this log.
  - Added `PROJECT_EXPLANATION_GUIDE.md` plus HTML/PDF exports for easy project explanation and Windows Task Manager comparison.

## Verification

Most recent successful command:

```powershell
dotnet build -c Release
```

Result:

- Build succeeded.
- `0 warnings`
- `0 errors`

Debug build can fail if the app is already running because the Debug exe is locked.

## Continue From Here

Useful next improvements:

- Add per-process GPU columns if needed.
- Add export/reporting for history summaries.
- Add startup change monitor.
- Add suspicious process scoring.
- Split `MainForm.cs` into partial files by tab if the user wants a larger structural cleanup.
