using System;
using System.IO;
using System.Text.Json;

namespace NetUpgradePlanner.Services;

internal static class SettingsService
{
    public static T? LoadValue<T>(string settingName, T defaultValue)
    {
        var path = GetSettingsPath(settingName);
        if (!File.Exists(path))
            return defaultValue;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    public static void StoreValue<T>(string settingName, T value)
    {
        var path = GetSettingsPath(settingName);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize<T>(value);
        File.WriteAllText(path, json);
    }

    public static bool IsConfigured(string settingName)
    {
        var path = GetSettingsPath(settingName);
        return File.Exists(path);
    }

    private static string GetSettingsPath(string settingName)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Join(appDataPath, "Terrajobst", "NetUpgradePlanner", "Settings", settingName + ".json");
    }
}
