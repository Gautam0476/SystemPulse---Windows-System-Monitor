# Distribution Guide

This is a Windows desktop app, so it must run on the Windows laptop/desktop whose processes you want to monitor.

## Best Sharing Format

Publish a self-contained single-file Windows executable so another user does not need to install .NET.

```powershell
cd D:\CustomTaskManager
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Output:

```text
D:\CustomTaskManager\bin\Release\net8.0-windows\win-x64\publish\CustomTaskManager.exe
```

## How To Share

- Simple: share `CustomTaskManager.exe`.
- Safer: zip the full `publish` folder and share the zip.

## User Run Steps

1. Download/extract.
2. Double-click `CustomTaskManager.exe`.
3. If SmartScreen appears, click `More info` then `Run anyway`.

Reason for warning: the app is unsigned. A polished release should be code-signed.

## Admin Notes

Normal monitoring works without admin. Admin may be needed for protected processes or all-users startup entries.

## Later Polish

- Add icon and version info.
- Create installer with Inno Setup or MSIX.
- Code-sign the exe.
- Add README and screenshots.
