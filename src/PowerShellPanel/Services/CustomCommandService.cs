using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PowerShellPanel.Models;

namespace PowerShellPanel.Services;

/// <summary>
/// Persists user-created custom commands to a local JSON file.
/// </summary>
public class CustomCommandService
{
    public static CustomCommandService Instance { get; } = new();

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "PowerShellPanel", "custom_commands.json");

    private List<UserCommand> _commands = new();

    public IReadOnlyList<UserCommand> Commands => _commands;

    private CustomCommandService()
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
                _commands = JsonSerializer.Deserialize<List<UserCommand>>(json) ?? new();
            }
        }
        catch { _commands = new(); }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_commands, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* fail silently */ }
    }

    public void Add(UserCommand cmd)
    {
        cmd.Id = Guid.NewGuid().ToString("N")[..8];
        _commands.Add(cmd);
        Save();
    }

    public void Update(UserCommand updated)
    {
        var existing = _commands.FirstOrDefault(c => c.Id == updated.Id);
        if (existing != null)
        {
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Category = updated.Category;
            existing.PowerShellCommand = updated.PowerShellCommand;
            existing.Parameters = updated.Parameters;
            Save();
        }
    }

    public void Delete(string id)
    {
        _commands.RemoveAll(c => c.Id == id);
        Save();
    }
}
