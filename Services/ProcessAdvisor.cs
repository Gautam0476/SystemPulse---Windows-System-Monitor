using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class ProcessAdvisor
{
    private static readonly HashSet<string> CriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "audiodg",
        "conhost",
        "csrss",
        "dwm",
        "explorer",
        "fontdrvhost",
        "idle",
        "lsass",
        "memory compression",
        "registry",
        "runtimebroker",
        "searchhost",
        "securityhealthservice",
        "services",
        "shellexperiencehost",
        "smss",
        "spoolsv",
        "startmenuexperiencehost",
        "svchost",
        "system",
        "taskhostw",
        "taskmgr",
        "wininit",
        "winlogon",
        "wmiprvse"
    };

    private static readonly HashSet<string> CommonUserApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "brave",
        "chrome",
        "code",
        "discord",
        "firefox",
        "msedge",
        "notepad",
        "obs64",
        "outlook",
        "photoshop",
        "spotify",
        "steam",
        "teams",
        "telegram",
        "whatsapp",
        "winword",
        "zoom"
    };

    public IReadOnlyList<ProcessRecommendation> RecommendMemoryClosures(IEnumerable<ProcessSnapshot> snapshots, int maxItems = 3)
    {
        return snapshots
            .Where(IsCandidate)
            .Select(CreateRecommendation)
            .Where(recommendation => recommendation.Score > 0)
            .OrderByDescending(recommendation => recommendation.Score)
            .Take(maxItems)
            .ToList();
    }

    private static bool IsCandidate(ProcessSnapshot snapshot)
    {
        if (snapshot.Id == Environment.ProcessId ||
            snapshot.Id <= 4 ||
            snapshot.MemoryMb < 96 ||
            CriticalProcessNames.Contains(snapshot.Name))
        {
            return false;
        }

        if (IsWindowsSystemPath(snapshot.Path))
        {
            return false;
        }

        return snapshot.MemoryMb >= 150 ||
            snapshot.CpuPercent >= 10 ||
            IsNotResponding(snapshot);
    }

    private static ProcessRecommendation CreateRecommendation(ProcessSnapshot snapshot)
    {
        var likelyUserApp = IsLikelyUserApp(snapshot);
        var notResponding = IsNotResponding(snapshot);
        var score = snapshot.MemoryMb;

        if (likelyUserApp)
        {
            score += 180;
        }
        else
        {
            score -= 120;
        }

        if (notResponding)
        {
            score += 450;
        }

        score += Math.Min(snapshot.CpuPercent * 28, 240);

        if (snapshot.ThreadCount > 80)
        {
            score += 50;
        }

        if (snapshot.Path.Length == 0)
        {
            score -= 80;
        }

        var reasonParts = new List<string>
        {
            $"{snapshot.MemoryMb:N0} MB RAM"
        };

        if (snapshot.CpuPercent >= 10)
        {
            reasonParts.Add($"{snapshot.CpuPercent:N1}% CPU");
        }

        if (notResponding)
        {
            reasonParts.Add("not responding");
        }

        reasonParts.Add(likelyUserApp ? "looks like a user app" : "review manually");

        return new ProcessRecommendation
        {
            ProcessId = snapshot.Id,
            ProcessName = snapshot.Name,
            EstimatedMemoryMb = Math.Round(snapshot.MemoryMb),
            Score = Math.Round(score, 1),
            Reason = string.Join(", ", reasonParts),
            SafetyNote = "Save work before closing."
        };
    }

    private static bool IsLikelyUserApp(ProcessSnapshot snapshot)
    {
        if (CommonUserApps.Contains(snapshot.Name))
        {
            return true;
        }

        return snapshot.Path.Contains(@"\Program Files\", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Path.Contains(@"\Program Files (x86)\", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Path.Contains(@"\Users\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWindowsSystemPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return windowsDirectory.Length > 0 &&
            path.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotResponding(ProcessSnapshot snapshot)
    {
        return snapshot.Status.Contains("Not responding", StringComparison.OrdinalIgnoreCase);
    }
}
