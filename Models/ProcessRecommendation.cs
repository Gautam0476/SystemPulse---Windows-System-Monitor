namespace CustomTaskManager.Models;

public sealed class ProcessRecommendation
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public double EstimatedMemoryMb { get; init; }

    public double Score { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string SafetyNote { get; init; } = string.Empty;
}
