using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PowerShellPanel.Models;
using PowerShellPanel.Services;

namespace PowerShellPanel.Views;

public partial class ParameterDialog : Window
{
    private readonly CommandItem _command;
    private readonly Dictionary<string, Control> _inputs = new();

    /// <summary>The assembled command string after user clicks OK; null if cancelled.</summary>
    public string? ResultCommand { get; private set; }

    public ParameterDialog(CommandItem command)
    {
        InitializeComponent();
        _command = command;

        DialogTitle.Text = command.Name;
        CommandDesc.Text = command.Description;

        BuildParameterControls();
    }

    /// <summary>
    /// Dynamically create input controls for each parameter definition.
    /// </summary>
    private void BuildParameterControls()
    {
        foreach (var param in _command.Parameters)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            // Label
            var label = new TextBlock
            {
                Text = param.Required ? $"{param.Label} *" : param.Label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1b)),
                Margin = new Thickness(0, 0, 0, 4),
            };
            row.Children.Add(label);

            // Input control based on type
            Control input;
            switch (param.Type)
            {
                case ParameterType.Dropdown:
                    input = CreateDropdown(param);
                    break;
                case ParameterType.Switch:
                    input = CreateSwitch(param);
                    break;
                case ParameterType.Number:
                    input = CreateTextBox(param, "123");
                    break;
                default:
                    input = CreateTextBox(param, param.Placeholder);
                    break;
            }

            row.Children.Add(input);
            _inputs[param.Key] = input;
            ParametersPanel.Children.Add(row);
        }
    }

    private Control CreateTextBox(CommandParameter param, string placeholder)
    {
        var tb = new TextBox
        {
            Text = param.DefaultValue,
            FontSize = 13,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            Padding = new Thickness(10, 7, 10, 7),
            Background = new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf7)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xe4, 0xe4, 0xe7)),
            BorderThickness = new Thickness(1),
        };

        // Apply CornerRadius via a Style targeting the Border in the template
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(6)));
        tb.Resources.Add(typeof(Border), style);

        // Placeholder text via a visual hint
        if (!string.IsNullOrEmpty(placeholder))
        {
            ToolTipService.SetToolTip(tb, placeholder);
        }

        return tb;
    }

    private Control CreateDropdown(CommandParameter param)
    {
        var cb = new ComboBox
        {
            FontSize = 13,
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf7)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xe4, 0xe4, 0xe7)),
            BorderThickness = new Thickness(1),
            IsEditable = false,
        };

        if (param.Choices != null)
        {
            foreach (var choice in param.Choices)
                cb.Items.Add(choice);

            if (!string.IsNullOrEmpty(param.DefaultValue) && param.Choices.Contains(param.DefaultValue))
                cb.SelectedItem = param.DefaultValue;
            else if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
        }

        return cb;
    }

    private Control CreateSwitch(CommandParameter param)
    {
        var cb = new CheckBox
        {
            Content = param.SwitchFlag ?? param.Label,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1b)),
            IsChecked = bool.TryParse(param.DefaultValue, out var dv) && dv,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        return cb;
    }

    /// <summary>
    /// Collect all parameter values from the input controls.
    /// </summary>
    private Dictionary<string, string> CollectValues()
    {
        var values = new Dictionary<string, string>();
        foreach (var (key, control) in _inputs)
        {
            values[key] = control switch
            {
                TextBox tb => tb.Text,
                ComboBox cb => cb.SelectedItem?.ToString() ?? "",
                CheckBox chk => chk.IsChecked == true ? "true" : "false",
                _ => "",
            };
        }
        return values;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        var values = CollectValues();
        foreach (var param in _command.Parameters)
        {
            if (param.Required && param.Type != ParameterType.Switch)
            {
                var val = values.GetValueOrDefault(param.Key, "");
                if (string.IsNullOrWhiteSpace(val))
                {
                    var msg = $"{LocalizationService.Get("Dialog.RequiredWarning")}「{param.Label}」";
                    MessageBox.Show(msg, LocalizationService.Get("Dialog.RequiredTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        ResultCommand = _command.BuildCommand(values);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }
}
