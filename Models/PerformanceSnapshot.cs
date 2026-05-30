namespace CustomTaskManager.Models;

public sealed class PerformanceSnapshot
{
    public double CpuPercent { get; init; }

    public double MemoryPercent { get; init; }

    public double MemoryUsedGb { get; init; }

    public double MemoryTotalGb { get; init; }

    public double MemoryAvailableGb { get; init; }

    public int ProcessCount { get; init; }

    public int ThreadCount { get; init; }

    public int HandleCount { get; init; }

    public int ProcessorCount { get; init; }

    public TimeSpan Uptime { get; init; }

    public string OsVersion { get; init; } = string.Empty;

    public BatterySnapshot Battery { get; init; } = new();

    public GpuSnapshot Gpu { get; init; } = new();
}
