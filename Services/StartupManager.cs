using System.Text;
using CustomTaskManager.Models;
using Microsoft.Win32;

namespace CustomTaskManager.Services;

public sealed class StartupManager
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DisabledRootPath = @"Software\CustomTaskManager\DisabledStartup";

    public List<StartupEntry> GetEntries()
    {
        var entries = new List<StartupEntry>();

        entries.AddRange(ReadRunEntries(RegistryHive.CurrentUser, RegistryView.Default, "Current user"));
        entries.AddRange(ReadRunEntries(RegistryHive.LocalMachine, RegistryView.Registry64, "All users"));
        entries.AddRange(ReadRunEntries(RegistryHive.LocalMachine, RegistryView.Registry32, "All users (32-bit)"));
        entries.AddRange(ReadDisabledEntries());

        return entries
            .GroupBy(entry => entry.Id)
            .Select(group => group.First())
            .OrderByDescending(entry => entry.Enabled)
            .ThenBy(entry => entry.Scope)
            .ThenBy(entry => entry.Name)
            .ToList();
    }

    public void Disable(StartupEntry entry)
    {
        if (!entry.Enabled)
        {
            return;
        }

        using var baseKey = RegistryKey.OpenBaseKey(entry.Hive, entry.View);
        using var runKey = baseKey.OpenSubKey(RunPath, writable: true)
            ?? throw new InvalidOperationException("Startup registry key was not found.");

        var value = runKey.GetValue(entry.Name);
        if (value is null)
        {
            throw new InvalidOperationException("Startup entry was already removed.");
        }

        using var disabledKey = Registry.CurrentUser.CreateSubKey($@"{DisabledRootPath}\{Encode(entry.Id)}", writable: true)
            ?? throw new InvalidOperationException("Could not create disabled startup backup.");

        disabledKey.SetValue("Id", entry.Id);
        disabledKey.SetValue("Name", entry.Name);
        disabledKey.SetValue("Command", value.ToString() ?? string.Empty);
        disabledKey.SetValue("Hive", entry.Hive.ToString());
        disabledKey.SetValue("View", entry.View.ToString());
        disabledKey.SetValue("Scope", entry.Scope);
        disabledKey.SetValue("RegistryPath", entry.RegistryPath);
        disabledKey.SetValue("DisabledAtUtc", DateTime.UtcNow.ToString("O"));

        runKey.DeleteValue(entry.Name, throwOnMissingValue: false);
    }

    public void Enable(StartupEntry entry)
    {
        if (entry.Enabled)
        {
            return;
        }

        using var disabledKey = Registry.CurrentUser.OpenSubKey($@"{DisabledRootPath}\{entry.BackupKeyName}", writable: true)
            ?? throw new InvalidOperationException("Disabled startup backup was not found.");

        var name = disabledKey.GetValue("Name")?.ToString();
        var command = disabledKey.GetValue("Command")?.ToString();
        var hiveText = disabledKey.GetValue("Hive")?.ToString();
        var viewText = disabledKey.GetValue("View")?.ToString();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command) ||
            !Enum.TryParse(hiveText, out RegistryHive hive) ||
            !Enum.TryParse(viewText, out RegistryView view))
        {
            throw new InvalidOperationException("Disabled startup backup is incomplete.");
        }

        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var runKey = baseKey.CreateSubKey(RunPath, writable: true)
            ?? throw new InvalidOperationException("Could not open startup registry key.");

        runKey.SetValue(name, command, RegistryValueKind.String);
        Registry.CurrentUser.DeleteSubKeyTree($@"{DisabledRootPath}\{entry.BackupKeyName}", throwOnMissingSubKey: false);
    }

    private static IEnumerable<StartupEntry> ReadRunEntries(RegistryHive hive, RegistryView view, string scope)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var runKey = baseKey.OpenSubKey(RunPath, writable: false);

        if (runKey is null)
        {
            yield break;
        }

        foreach (var valueName in runKey.GetValueNames())
        {
            var command = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
            var id = BuildId(hive, view, valueName);

            yield return new StartupEntry
            {
                Id = id,
                Name = valueName,
                Command = command,
                Scope = scope,
                RegistryPath = $@"{hive}\{RunPath}",
                Hive = hive,
                View = view,
                Enabled = true
            };
        }
    }

    private static IEnumerable<StartupEntry> ReadDisabledEntries()
    {
        using var root = Registry.CurrentUser.OpenSubKey(DisabledRootPath, writable: false);

        if (root is null)
        {
            yield break;
        }

        foreach (var backupKeyName in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(backupKeyName, writable: false);
            if (key is null)
            {
                continue;
            }

            var hiveText = key.GetValue("Hive")?.ToString();
            var viewText = key.GetValue("View")?.ToString();

            if (!Enum.TryParse(hiveText, out RegistryHive hive) ||
                !Enum.TryParse(viewText, out RegistryView view))
            {
                continue;
            }

            yield return new StartupEntry
            {
                Id = key.GetValue("Id")?.ToString() ?? backupKeyName,
                BackupKeyName = backupKeyName,
                Name = key.GetValue("Name")?.ToString() ?? backupKeyName,
                Command = key.GetValue("Command")?.ToString() ?? string.Empty,
                Scope = key.GetValue("Scope")?.ToString() ?? "Disabled",
                RegistryPath = key.GetValue("RegistryPath")?.ToString() ?? RunPath,
                Hive = hive,
                View = view,
                Enabled = false
            };
        }
    }

    private static string BuildId(RegistryHive hive, RegistryView view, string name)
    {
        return $"{hive}|{view}|{name}";
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
