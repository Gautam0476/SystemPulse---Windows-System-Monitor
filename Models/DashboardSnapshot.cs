namespace CustomTaskManager.Models;

public sealed class DashboardSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public PerformanceSnapshot Performance { get; init; } = new();

    public List<DashboardProcess> TopCpu { get; init; } = [];

    public List<DashboardProcess> TopMemory { get; init; } = [];

    public static DashboardSnapshot Empty { get; } = new();
}

public sealed class DashboardProcess
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public double CpuPercent { get; init; }

    public double MemoryMb { get; init; }

    public int ThreadCount { get; init; }

    public string Status { get; init; } = string.Empty;
}
