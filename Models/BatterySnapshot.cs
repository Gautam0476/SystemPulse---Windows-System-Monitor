namespace CustomTaskManager.Models;

public sealed class BatterySnapshot
{
    public bool IsBatteryPresent { get; init; }

    public double? ChargePercent { get; init; }

    public string ChargeStatus { get; init; } = "Unknown";

    public string PowerLineStatus { get; init; } = "Unknown";

    public TimeSpan? LifeRemaining { get; init; }

    public string Summary
    {
        get
        {
            if (!IsBatteryPresent)
            {
                return "No battery";
            }

            var percent = ChargePercent is null ? "Unknown" : $"{ChargePercent:N0}%";
            return $"{percent} - {ChargeStatus}";
        }
    }
}
