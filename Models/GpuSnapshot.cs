namespace CustomTaskManager.Models;

public sealed class GpuSnapshot
{
    public bool IsAvailable { get; init; }

    public double UsagePercent { get; init; }

    public string Source { get; init; } = "Windows GPU Engine counters";

    public string Error { get; init; } = string.Empty;

    public string Summary => IsAvailable
        ? $"{UsagePercent:N1}%"
        : "Unavailable";
}
