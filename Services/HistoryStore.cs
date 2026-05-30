using System.Globalization;
using System.Text;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class HistoryStore
{
    private readonly string _path;

    public HistoryStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CustomTaskManager");

        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "metrics.csv");
    }

    public void Append(DateTime timestamp, IEnumerable<ProcessSnapshot> snapshots)
    {
        var newFile = !File.Exists(_path);

        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream, Encoding.UTF8);

        if (newFile)
        {
            writer.WriteLine("Timestamp,Pid,Name,CpuPercent,MemoryMb");
        }

        foreach (var snapshot in snapshots)
        {
            writer.WriteLine(string.Join(",",
                timestamp.ToString("O", CultureInfo.InvariantCulture),
                snapshot.Id.ToString(CultureInfo.InvariantCulture),
                Escape(snapshot.Name),
                snapshot.CpuPercent.ToString("F1", CultureInfo.InvariantCulture),
                snapshot.MemoryMb.ToString("F1", CultureInfo.InvariantCulture)));
        }
    }

    public List<HistorySummary> GetSummary(TimeSpan window)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        var since = DateTime.Now - window;
        var groups = new Dictionary<string, HistoryAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(_path).Skip(1))
        {
            var parts = ParseCsvLine(line);
            if (parts.Count < 5)
            {
                continue;
            }

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp) ||
                timestamp < since ||
                !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var cpu) ||
                !double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var memory))
            {
                continue;
            }

            var name = parts[2];
            if (!groups.TryGetValue(name, out var accumulator))
            {
                accumulator = new HistoryAccumulator();
                groups[name] = accumulator;
            }

            accumulator.Samples++;
            accumulator.TotalCpu += cpu;
            accumulator.TotalMemory += memory;
            accumulator.MaxMemory = Math.Max(accumulator.MaxMemory, memory);
        }

        return groups
            .Select(group => new HistorySummary
            {
                Name = group.Key,
                Samples = group.Value.Samples,
                AverageCpu = Math.Round(group.Value.TotalCpu / group.Value.Samples, 1),
                AverageMemoryMb = Math.Round(group.Value.TotalMemory / group.Value.Samples, 1),
                MaxMemoryMb = Math.Round(group.Value.MaxMemory, 1)
            })
            .OrderByDescending(summary => summary.AverageCpu)
            .ThenByDescending(summary => summary.AverageMemoryMb)
            .Take(100)
            .ToList();
    }

    private static string Escape(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }

    private sealed class HistoryAccumulator
    {
        public int Samples { get; set; }

        public double TotalCpu { get; set; }

        public double TotalMemory { get; set; }

        public double MaxMemory { get; set; }
    }
}
