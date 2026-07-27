using System.Collections.Generic;

namespace PowerShellPanel.Models;

/// <summary>
/// A user-created custom command, persisted to disk.
/// </summary>
public class UserCommand
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "⭐ My Commands";
    public string PowerShellCommand { get; set; } = "";
    public List<CommandParameter> Parameters { get; set; } = new();
}
