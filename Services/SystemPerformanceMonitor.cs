using System.Runtime.InteropServices;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class SystemPerformanceMonitor
{
    private readonly BatteryMonitor _batteryMonitor = new();
    private readonly GpuMonitor _gpuMonitor = new();
    private CpuTimes? _lastCpuTimes;

    public PerformanceSnapshot Capture(IReadOnlyCollection<ProcessSnapshot> processes)
    {
        var memory = GetMemoryStatus();

        return new PerformanceSnapshot
        {
            CpuPercent = Math.Round(GetCpuPercent(), 1),
            MemoryPercent = Math.Round((double)memory.MemoryLoad, 1),
            MemoryUsedGb = Math.Round((memory.TotalPhys - memory.AvailPhys) / 1024d / 1024d / 1024d, 1),
            MemoryTotalGb = Math.Round(memory.TotalPhys / 1024d / 1024d / 1024d, 1),
            MemoryAvailableGb = Math.Round(memory.AvailPhys / 1024d / 1024d / 1024d, 1),
            ProcessCount = processes.Count,
            ThreadCount = processes.Sum(process => process.ThreadCount),
            HandleCount = processes.Sum(process => process.HandleCount),
            ProcessorCount = Environment.ProcessorCount,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            OsVersion = Environment.OSVersion.VersionString,
            Battery = _batteryMonitor.Capture(),
            Gpu = _gpuMonitor.Capture()
        };
    }

    private double GetCpuPercent()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0;
        }

        var current = new CpuTimes(ToUInt64(idleTime), ToUInt64(kernelTime), ToUInt64(userTime));

        if (_lastCpuTimes is null)
        {
            _lastCpuTimes = current;
            return 0;
        }

        var previous = _lastCpuTimes.Value;
        _lastCpuTimes = current;

        var idleDelta = current.Idle - previous.Idle;
        var kernelDelta = current.Kernel - previous.Kernel;
        var userDelta = current.User - previous.User;
        var totalDelta = kernelDelta + userDelta;

        if (totalDelta == 0 || idleDelta > totalDelta)
        {
            return 0;
        }

        return Math.Clamp((totalDelta - idleDelta) * 100d / totalDelta, 0, 100);
    }

    private static MemoryStatus GetMemoryStatus()
    {
        var status = new MemoryStatus
        {
            Length = (uint)Marshal.SizeOf<MemoryStatus>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            return new MemoryStatus { Length = status.Length };
        }

        return status;
    }

    private static ulong ToUInt64(FileTime fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;

        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;

        public uint MemoryLoad;

        public ulong TotalPhys;

        public ulong AvailPhys;

        public ulong TotalPageFile;

        public ulong AvailPageFile;

        public ulong TotalVirtual;

        public ulong AvailVirtual;

        public ulong AvailExtendedVirtual;
    }

    private readonly record struct CpuTimes(ulong Idle, ulong Kernel, ulong User);
}
