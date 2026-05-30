# Differentiating Feature Ideas

Date: 29 May 2026

Purpose: Is file mein woh feature ideas hain jo custom task manager ko Windows ke inbuilt Task Manager se genuinely different bana sakte hain.

## Strong Feature Ideas

### 1. Suspicious Process Detector With Custom Scoring

App unknown or risky processes ko custom score ke saath highlight kare.

Signals:

- Process running from `Temp`, `Downloads`, or suspicious folder
- No executable path available
- Very high CPU/RAM usage
- Unknown publisher
- Random-looking process name
- New process never seen before on this system
- Process added itself to startup recently
- Process repeatedly restarts after being killed

Why different:

- Basic suspicious/malware detection Windows Security side par hota hai, aur Task Manager publisher/path jaise clues dikha sakta hai.
- Isliye sirf "suspicious process dikhao" strong unique nahi hai.
- Strong version tab hoga jab custom app apna explainable score, history, user labels, startup-change link, and custom rules combine kare.

Interview-safe line:

> This is not meant to replace Windows Security. It is a custom explainable process scoring system that combines path, usage, startup changes, and local history to help the user review unusual processes.

### 2. Process Notes And Labels

User kisi process ko label/note de sake.

Examples:

- `chrome.exe` -> Browser
- `Code.exe` -> VS Code
- `unknown.exe` -> Check later

Why different:

- Task Manager user notes nahi rakhta.
- Custom app personal process knowledge base ban sakta hai.

### 3. Smart Alerts

App notification de jab:

- CPU 90% se upar 1 minute tak rahe
- RAM 85% cross kare
- New unknown startup entry add ho
- Koi process repeatedly crash/restart ho

Why different:

- Task Manager mostly manual checking tool hai.
- Smart alerts app ko proactive monitoring utility banate hain.

### 4. App Usage Timeline

Show kare ki day mein kaunsi apps kitni der active/running rahi.

Examples:

- Chrome: 4 hours
- VS Code: 3 hours
- Discord: 1 hour

Why different:

- Task Manager live usage dikhata hai.
- Timeline productivity/analysis feature ban jaata hai.

### 5. Resource Budget Per App

User app-wise limit set kare.

Examples:

- Chrome max RAM budget: 3 GB
- Game max background CPU: 20%
- Discord max RAM: 800 MB

Why different:

- Task Manager limit/budget concept nahi deta.
- This works with automation rules and alerts.

### 6. Startup Change Monitor

App detect kare ki koi new startup item add hua hai.

Features:

- New startup entry alert
- Startup entry added time
- Old/new startup list comparison
- Suspicious startup highlight

Why different:

- Startup disable alone different nahi hai.
- Startup change monitoring is genuinely useful and more unique.

### 7. Weekly Performance Report

App weekly report generate kare.

Report can include:

- Top CPU consumers
- Top RAM consumers
- Most frequently running apps
- Startup changes
- Performance bottleneck suggestions

Why different:

- Task Manager report generation nahi karta.

Current related progress:

- `Visualization` tab implemented with overall pressure pie chart.
- It combines CPU, GPU, memory, battery risk, and process/thread/handle pressure.
- This can later become a weekly report/charting feature.

### 8. One-Click Modes

User modes bana sake:

- Gaming Mode: heavy background apps kill/pause
- Study Mode: distracting apps alert/kill
- Work Mode: required apps open, unnecessary startup apps off

Why different:

- Task Manager manual process control deta hai.
- Modes custom workflow automation dete hain.

### 9. Remote Dashboard

Local machine ka process/performance status browser ya phone par dekh sake.

Current status:

- Basic version implemented.
- Access-key authentication implemented for live dashboard data.
- Browser/mobile dashboard UI polished with login screen, live status badge, metric cards, graphs, and cleaner process tables.
- App starts a local dashboard on port `5055`.
- Laptop browser can open `http://localhost:5055`.
- Phone on same Wi-Fi can open laptop IP plus port, example `http://192.168.1.14:5055`.
- Dashboard shows CPU, memory, battery, power, top CPU processes, and top RAM processes.

Why different:

- Windows Task Manager same machine par focused hai.
- Remote view project ko advanced bana dega.

Future improvements:

- Add QR code for phone access
- Add action buttons carefully, such as alert/kill only after confirmation
- Add historical charts from `HistoryStore`
- Add "reset access key" button in the Windows app

### 10. Process Reputation Cache

App known processes ki local reputation list rakhe.

Examples:

- Trusted
- Unknown
- Needs review
- Suspicious

Why different:

- Task Manager raw process list deta hai.
- Custom app user-friendly classification de sakta hai.

## Best 3 Features To Add Next

Recommended priority:

1. Smart Alerts / Notifications
2. Startup Change Monitor
3. Suspicious Process Detector With Custom Scoring

Reason:

- Ye features Task Manager se clearly different hain.
- Implementation manageable hai.
- Resume/interview mein explain karna easy hai.
- Existing codebase ke process monitor, rules, and startup manager ke upar build ho sakte hain.
- Remote Dashboard ka basic version already implemented hai, so next work should strengthen automation/alerts/history.
