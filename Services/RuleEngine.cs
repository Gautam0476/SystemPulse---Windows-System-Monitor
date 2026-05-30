using System.Diagnostics;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class RuleEngine
{
    private static readonly TimeSpan TriggerCooldown = TimeSpan.FromSeconds(60);

    public List<string> Evaluate(IEnumerable<AutomationRule> rules, IEnumerable<ProcessSnapshot> snapshots)
    {
        var now = DateTime.UtcNow;
        var messages = new List<string>();
        var snapshotList = snapshots.ToList();

        foreach (var rule in rules.Where(rule => rule.Enabled))
        {
            if (string.IsNullOrWhiteSpace(rule.ProcessNameContains))
            {
                continue;
            }

            if (rule.LastTriggeredUtc is not null && now - rule.LastTriggeredUtc < TriggerCooldown)
            {
                continue;
            }

            var matches = snapshotList
                .Where(snapshot => snapshot.Id != Environment.ProcessId)
                .Where(snapshot => snapshot.Name.Contains(rule.ProcessNameContains, StringComparison.OrdinalIgnoreCase))
                .Where(snapshot => GetMetric(snapshot, rule.Metric) >= rule.Threshold)
                .OrderByDescending(snapshot => GetMetric(snapshot, rule.Metric))
                .Take(5)
                .ToList();

            if (matches.Count == 0)
            {
                continue;
            }

            rule.LastTriggeredUtc = now;

            if (rule.Action == RuleAction.Alert)
            {
                var top = matches[0];
                messages.Add($"{top.Name} crossed {rule.Metric} threshold ({GetMetric(top, rule.Metric):N1} >= {rule.Threshold:N1}).");
                continue;
            }

            foreach (var match in matches)
            {
                try
                {
                    using var process = Process.GetProcessById(match.Id);
                    process.Kill(entireProcessTree: true);
                    messages.Add($"Killed {match.Name} ({match.Id}) by automation rule.");
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    messages.Add($"Could not kill {match.Name} ({match.Id}): {ex.Message}");
                }
            }
        }

        return messages;
    }

    private static double GetMetric(ProcessSnapshot snapshot, RuleMetric metric)
    {
        return metric == RuleMetric.CpuPercent ? snapshot.CpuPercent : snapshot.MemoryMb;
    }
}
