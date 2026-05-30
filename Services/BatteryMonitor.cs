using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class BatteryMonitor
{
    public BatterySnapshot Capture()
    {
        var power = SystemInformation.PowerStatus;
        var hasBattery = !power.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery);

        return new BatterySnapshot
        {
            IsBatteryPresent = hasBattery,
            ChargePercent = hasBattery ? Math.Clamp(power.BatteryLifePercent * 100d, 0, 100) : null,
            ChargeStatus = power.BatteryChargeStatus.ToString(),
            PowerLineStatus = power.PowerLineStatus.ToString(),
            LifeRemaining = hasBattery && power.BatteryLifeRemaining >= 0
                ? TimeSpan.FromSeconds(power.BatteryLifeRemaining)
                : null
        };
    }
}
