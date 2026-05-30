using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class DashboardState
{
    private readonly object _gate = new();
    private DashboardSnapshot _snapshot = DashboardSnapshot.Empty;

    public void Update(PerformanceSnapshot performance, IEnumerable<ProcessSnapshot> processes)
    {
        var processList = processes.ToList();

        var snapshot = new DashboardSnapshot
        {
            Timestamp = DateTime.Now,
            Performance = performance,
            TopCpu = processList
                .OrderByDescending(process => process.CpuPercent)
                .Take(10)
                .Select(ToDashboardProcess)
                .ToList(),
            TopMemory = processList
                .OrderByDescending(process => process.MemoryMb)
                .Take(10)
                .Select(ToDashboardProcess)
                .ToList()
        };

        lock (_gate)
        {
            _snapshot = snapshot;
        }
    }

    public DashboardSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return _snapshot;
        }
    }

    private static DashboardProcess ToDashboardProcess(ProcessSnapshot process)
    {
        return new DashboardProcess
        {
            Id = process.Id,
            Name = process.Name,
            CpuPercent = process.CpuPercent,
            MemoryMb = process.MemoryMb,
            ThreadCount = process.ThreadCount,
            Status = process.Status
        };
    }
}
