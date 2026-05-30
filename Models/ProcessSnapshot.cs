namespace CustomTaskManager.Models;

public sealed class ProcessSnapshot
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public double CpuPercent { get; init; }

    public double MemoryMb { get; init; }

    public int ThreadCount { get; init; }

    public int HandleCount { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime? StartTime { get; init; }

    public string ParentText => ParentId?.ToString() ?? string.Empty;

    public string StartTimeText => StartTime?.ToString("g") ?? string.Empty;
}
