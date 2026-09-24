using System;
using System.IO;
using System.Text.Json;

namespace Unbound.Core;

public sealed class UserSettings
{
    public bool ConfirmAutomaticUnlock { get; set; } = true;
    public bool ShowLockingApplications { get; set; } = true;
    public bool ShowOperationSummary { get; set; } = true;
    public bool DeleteAfterReboot { get; set; }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Unbound");

    public static string SettingsPath =>
        Path.Combine(SettingsDirectory, "settings.json");

    public static UserSettings Current { get; private set; } = Load();

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings are a convenience. The app should still work if Windows
            // blocks the settings file or a profile is temporarily unavailable.
        }
    }

    public static void Reset()
    {
        Current = new UserSettings();
        Save();
    }

    private static UserSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new UserSettings();

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }
}
