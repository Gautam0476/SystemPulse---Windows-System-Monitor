namespace CustomTaskManager.Models;

public sealed class HistorySummary
{
    public string Name { get; init; } = string.Empty;

    public int Samples { get; init; }

    public double AverageCpu { get; init; }

    public double AverageMemoryMb { get; init; }

    public double MaxMemoryMb { get; init; }
}
