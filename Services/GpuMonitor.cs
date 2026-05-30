using System.ComponentModel;
using System.Diagnostics;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class GpuMonitor
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(2);

    private DateTime _lastCaptureUtc = DateTime.MinValue;
    private GpuSnapshot _lastSnapshot = new();

    public GpuSnapshot Capture()
    {
        if (DateTime.UtcNow - _lastCaptureUtc < CacheWindow)
        {
            return _lastSnapshot;
        }

        _lastCaptureUtc = DateTime.UtcNow;
        _lastSnapshot = CaptureCore();
        return _lastSnapshot;
    }

    private static GpuSnapshot CaptureCore()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists("GPU Engine"))
            {
                return Unavailable("GPU Engine counters not found.");
            }

            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames()
                .Where(instance => instance.Contains("engtype_", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (instances.Length == 0)
            {
                return Unavailable("No GPU engine instances found.");
            }

            double total = 0;
            var readableCounters = 0;

            foreach (var instance in instances)
            {
                try
                {
                    using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance, true);
                    var value = counter.NextValue();
                    if (!float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        total += value;
                        readableCounters++;
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or Win32Exception)
                {
                    // GPU engine instances can appear/disappear while processes start or exit.
                }
            }

            if (readableCounters == 0)
            {
                return Unavailable("GPU counters were not readable.");
            }

            return new GpuSnapshot
            {
                IsAvailable = true,
                UsagePercent = Math.Round(Math.Clamp(total, 0, 100), 1)
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or PlatformNotSupportedException or Win32Exception)
        {
            return Unavailable(ex.Message);
        }
    }

    private static GpuSnapshot Unavailable(string error)
    {
        return new GpuSnapshot
        {
            IsAvailable = false,
            Error = error
        };
    }
}
