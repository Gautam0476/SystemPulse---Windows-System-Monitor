namespace CustomTaskManager.Models;

public enum RuleMetric
{
    CpuPercent,
    MemoryMb
}

public enum RuleAction
{
    Alert,
    KillProcess
}

public sealed class AutomationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool Enabled { get; set; } = true;

    public string ProcessNameContains { get; set; } = string.Empty;

    public RuleMetric Metric { get; set; } = RuleMetric.MemoryMb;

    public double Threshold { get; set; } = 1024;

    public RuleAction Action { get; set; } = RuleAction.Alert;

    public DateTime? LastTriggeredUtc { get; set; }

    public string LastTriggeredText => LastTriggeredUtc?.ToLocalTime().ToString("g") ?? string.Empty;
}
