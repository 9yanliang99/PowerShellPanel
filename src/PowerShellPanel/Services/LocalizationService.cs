using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PowerShellPanel.Services;

/// <summary>
/// Singleton localization service. Loads JSON resource files and
/// supports runtime language switching via INotifyPropertyChanged.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    // ═══════════════════════════════════════════════════
    //  Singleton
    // ═══════════════════════════════════════════════════

    public static LocalizationService Instance { get; } = new();

    private LocalizationService()
    {
        _currentLang = SettingsService.Instance.Language;
        LoadResources(_currentLang);
    }

    // ═══════════════════════════════════════════════════
    //  Fields
    // ═══════════════════════════════════════════════════

    private string _currentLang;
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    public static readonly string[] SupportedLanguages = ["en", "zh-CN", "zh-TW"];

    public string CurrentLanguage
    {
        get => _currentLang;
        set
        {
            if (_currentLang == value) return;
            _currentLang = value;
            LoadResources(value);
            SettingsService.Instance.Language = value;
            SettingsService.Instance.Save();
            OnPropertyChanged(null); // notify all bindings
        }
    }

    // ═══════════════════════════════════════════════════
    //  Indexer — used by LocExtension bindings
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Returns the localized string for the given key.
    /// Falls back to the key itself if not found.
    /// </summary>
    public string this[string key] =>
        _strings.TryGetValue(key, out var value) ? value : key;

    /// <summary>
    /// Convenience: get a string from code.
    /// </summary>
    public static string Get(string key) => Instance[key];

    /// <summary>
    /// Convenience: get a string with formatting.
    /// </summary>
    public static string Get(string key, params object[] args) =>
        string.Format(Instance[key], args);

    // ═══════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════

    private void LoadResources(string lang)
    {
        _strings.Clear();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(baseDir, "Resources", $"strings.{lang}.json");

        if (!File.Exists(path))
        {
            // Fallback to English
            path = Path.Combine(baseDir, "Resources", "strings.en.json");
            if (!File.Exists(path)) return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                foreach (var (key, value) in dict)
                    _strings[key] = value;
            }
        }
        catch { /* keep defaults */ }
    }

    // ═══════════════════════════════════════════════════
    //  INotifyPropertyChanged
    // ═══════════════════════════════════════════════════

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
