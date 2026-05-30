using Microsoft.Win32;

namespace CustomTaskManager.Models;

public sealed class StartupEntry
{
    public string Id { get; init; } = string.Empty;

    public string BackupKeyName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Command { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;

    public string RegistryPath { get; init; } = string.Empty;

    public RegistryHive Hive { get; init; }

    public RegistryView View { get; init; }

    public bool Enabled { get; init; }

    public bool RequiresAdmin => Hive == RegistryHive.LocalMachine;

    public string State => Enabled ? "Enabled" : "Disabled";
}
