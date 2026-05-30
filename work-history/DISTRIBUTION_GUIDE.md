# Distribution Guide

Purpose: Custom Task Manager ko dusre Windows laptops par chalane ke liye packaging steps.

## Important Concept

Ye Windows desktop app hai, website nahi. Isliye app ko jis laptop ka process/RAM/CPU data monitor karna hai, usi laptop par run hona zaroori hai.

Hosting ka practical meaning:

- App ka `.exe` ya `.zip` file GitHub/Drive/website par upload karo.
- Dusra user download kare.
- Apne Windows laptop par run kare.

## Best Format For Sharing

Best beginner-friendly format:

- Self-contained single-file Windows executable

Benefit:

- Dusre laptop par .NET install hona zaroori nahi.
- User ko mostly sirf `.exe` run karni hogi.

## Publish Command

From project folder:

```powershell
cd D:\CustomTaskManager
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Output folder:

```text
D:\CustomTaskManager\bin\Release\net8.0-windows\win-x64\publish
```

Main file:

```text
CustomTaskManager.exe
```

## How To Share

Option 1:

- Share only `CustomTaskManager.exe` from publish folder.

Option 2:

- Zip the full `publish` folder and share the zip.

Safer recommendation:

- Zip the full `publish` folder.

## How User Runs It

User downloads/extracts it, then double-clicks:

```text
CustomTaskManager.exe
```

If Windows SmartScreen warning appears:

- Click `More info`
- Click `Run anyway`

Reason:

- App is unsigned. Production apps should be code-signed.

## Admin Permission Notes

Normal process monitoring should work without admin.

Some features may need admin:

- Killing protected/system processes
- Editing all-users startup entries
- Reading some protected process paths

If needed:

- Right click `CustomTaskManager.exe`
- Click `Run as administrator`

## Compatibility

Current recommended target:

- `win-x64`

Works on:

- Most Windows 10/11 laptops

Other possible targets:

```powershell
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true
```

Use:

- `win-x64` for normal modern laptops
- `win-x86` for old 32-bit Windows
- `win-arm64` for ARM Windows laptops

## Professional Distribution Later

For a more polished project:

- Create installer using Inno Setup
- Create MSIX package
- Code-sign the exe
- Add app icon
- Add version info
- Add README and screenshots
