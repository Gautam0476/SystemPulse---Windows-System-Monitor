using System.ComponentModel;
using System.Diagnostics;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class ProcessMonitor
{
    private readonly Dictionary<int, CpuSample> _cpuSamples = new();

    public List<ProcessSnapshot> Capture()
    {
        var now = DateTime.UtcNow;
        var parentMap = NativeProcess.GetParentProcessMap();
        var activePids = new HashSet<int>();
        var snapshots = new List<ProcessSnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var pid = process.Id;
                activePids.Add(pid);

                var totalCpu = ReadNullable(() => process.TotalProcessorTime);
                var cpuPercent = CalculateCpuPercent(pid, totalCpu, now);

                parentMap.TryGetValue(pid, out var parentPid);
                var memoryMb = ReadValue(() => process.WorkingSet64 / 1024d / 1024d, 0);
                var threadCount = ReadValue(() => process.Threads.Count, 0);
                var handleCount = ReadValue(() => process.HandleCount, 0);
                var startTime = ReadNullable(() => process.StartTime);
                var path = ReadString(() => process.MainModule?.FileName ?? string.Empty);
                var status = ReadValue(() => process.Responding ? "Responsive" : "Not responding", "Unknown");

                snapshots.Add(new ProcessSnapshot
                {
                    Id = pid,
                    ParentId = parentPid > 0 ? parentPid : null,
                    Name = process.ProcessName,
                    CpuPercent = Math.Round(cpuPercent, 1),
                    MemoryMb = Math.Round(memoryMb, 1),
                    ThreadCount = threadCount,
                    HandleCount = handleCount,
                    StartTime = startTime,
                    Path = path,
                    Status = status
                });
            }
            catch (InvalidOperationException)
            {
                // The process can exit while we are sampling it.
            }
            catch (Win32Exception)
            {
                // Some system processes deny access to selected fields.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var stalePid in _cpuSamples.Keys.Except(activePids).ToList())
        {
            _cpuSamples.Remove(stalePid);
        }

        return snapshots
            .OrderByDescending(process => process.CpuPercent)
            .ThenByDescending(process => process.MemoryMb)
            .ToList();
    }

    private double CalculateCpuPercent(int pid, TimeSpan? totalCpu, DateTime now)
    {
        if (totalCpu is null)
        {
            return 0;
        }

        if (!_cpuSamples.TryGetValue(pid, out var previous))
        {
            _cpuSamples[pid] = new CpuSample(totalCpu.Value, now);
            return 0;
        }

        var cpuDelta = (totalCpu.Value - previous.TotalProcessorTime).TotalMilliseconds;
        var timeDelta = (now - previous.TimestampUtc).TotalMilliseconds;
        _cpuSamples[pid] = new CpuSample(totalCpu.Value, now);

        if (timeDelta <= 0 || cpuDelta < 0)
        {
            return 0;
        }

        var percent = cpuDelta / (timeDelta * Environment.ProcessorCount) * 100;
        return Math.Clamp(percent, 0, 100);
    }

    private static T ReadValue<T>(Func<T> reader, T fallback)
    {
        try
        {
            return reader();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return fallback;
        }
    }

    private static T? ReadNullable<T>(Func<T> reader)
        where T : struct
    {
        try
        {
            return reader();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    private static string ReadString(Func<string> reader)
    {
        try
        {
            return reader();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return string.Empty;
        }
    }

    private sealed record CpuSample(TimeSpan TotalProcessorTime, DateTime TimestampUtc);
}
