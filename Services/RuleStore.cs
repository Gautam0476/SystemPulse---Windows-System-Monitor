using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustomTaskManager.Models;

namespace CustomTaskManager.Services;

public sealed class RuleStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RuleStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CustomTaskManager");

        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "rules.json");
    }

    public BindingList<AutomationRule> Load()
    {
        if (!File.Exists(_path))
        {
            return new BindingList<AutomationRule>();
        }

        try
        {
            var json = File.ReadAllText(_path);
            var rules = JsonSerializer.Deserialize<List<AutomationRule>>(json, _serializerOptions) ?? [];
            return new BindingList<AutomationRule>(rules);
        }
        catch (JsonException)
        {
            return new BindingList<AutomationRule>();
        }
        catch (IOException)
        {
            return new BindingList<AutomationRule>();
        }
    }

    public void Save(IEnumerable<AutomationRule> rules)
    {
        var json = JsonSerializer.Serialize(rules.ToList(), _serializerOptions);
        File.WriteAllText(_path, json);
    }
}
