using System;
using System.IO;
using System.Text.Json;

namespace PowerShellPanel.Services;

/// <summary>
/// Simple JSON-based settings persistence.
/// </summary>
public class SettingsService
{
    public static SettingsService Instance { get; } = new();

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "PowerShellPanel", "settings.json");

    public string Language { get; set; } = "en";

    private SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data?.Language != null)
                    Language = data.Language;
            }
        }
        catch { /* use defaults */ }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new SettingsData { Language = Language };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* fail silently */ }
    }

    private class SettingsData
    {
        public string Language { get; set; } = "en";
    }
}
