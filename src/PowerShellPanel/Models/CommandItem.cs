namespace PowerShellPanel.Models;

/// <summary>
/// A single PowerShell command that can be executed from the UI panel.
/// If Parameters is non-empty, clicking the card opens a parameter dialog first.
/// </summary>
public class CommandItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The PowerShell command template. Use {key} placeholders for parameters,
    /// e.g. "ping {target} -n {count}". If no parameters, the command runs as-is.
    /// </summary>
    public string PowerShellCommand { get; set; } = string.Empty;

    public bool IsDangerous { get; set; }

    /// <summary>
    /// User-configurable parameters. Empty = no dialog; just fill the command directly.
    /// </summary>
    public List<CommandParameter> Parameters { get; set; } = new();

    /// <summary>Convenience: does this command need a parameter dialog?</summary>
    public bool HasParameters => Parameters.Count > 0;

    /// <summary>
    /// Assemble the final command by replacing {key} placeholders with values.
    /// Switch parameters with false values are removed.
    /// </summary>
    public string BuildCommand(Dictionary<string, string> values)
    {
        var result = PowerShellCommand;
        foreach (var p in Parameters)
        {
            var val = values.GetValueOrDefault(p.Key, p.DefaultValue);
            if (p.Type == ParameterType.Switch)
            {
                // Switch: if "true", insert the flag text; otherwise remove the placeholder
                if (bool.TryParse(val, out var on) && on && p.SwitchFlag != null)
                    result = result.Replace($"{{{p.Key}}}", p.SwitchFlag);
                else
                    result = result.Replace($" {{{p.Key}}}", "").Replace($"{{{p.Key}}}", "");
            }
            else
            {
                result = result.Replace($"{{{p.Key}}}", val);
            }
        }
        return result.Trim();
    }
}
