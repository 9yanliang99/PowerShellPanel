namespace PowerShellPanel.Models;

/// <summary>
/// Defines a user-configurable parameter for a PowerShell command.
/// When a command has parameters, clicking its card opens a dialog
/// where the user fills in values before the command is assembled.
/// </summary>
public class CommandParameter
{
    /// <summary>Placeholder key in the command template, e.g. "target".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display label in the dialog, e.g. "目标地址".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Placeholder / hint inside the input field.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Default value pre-filled in the input field.</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>Whether this parameter must be filled before OK is enabled.</summary>
    public bool Required { get; set; } = true;

    /// <summary>Parameter type: Text, Number, Dropdown, Switch (bool flag).</summary>
    public ParameterType Type { get; set; } = ParameterType.Text;

    /// <summary>For Dropdown type: the available choices.</summary>
    public List<string>? Choices { get; set; }

    /// <summary>For Switch type: the flag text inserted when enabled, e.g. "-Force".</summary>
    public string? SwitchFlag { get; set; }
}

public enum ParameterType
{
    Text,
    Number,
    Dropdown,
    Switch
}
