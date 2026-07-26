using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using PowerShellPanel.Services;

namespace PowerShellPanel.Extensions;

/// <summary>
/// XAML markup extension for localization.
/// Usage: Text="{l:Loc KeyName}"
/// Creates a one-way binding to LocalizationService.Instance[key].
/// </summary>
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding
        {
            // Use indexer on the singleton instance
            Source = LocalizationService.Instance,
            Path = new PropertyPath($"[{Key}]"),
            Mode = BindingMode.OneWay,
            FallbackValue = Key, // show key if missing
        };
        return binding.ProvideValue(serviceProvider);
    }
}
