using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PowerShellPanel.Models;
using PowerShellPanel.Services;

namespace PowerShellPanel.Views;

public partial class CommandEditorDialog : Window
{
    private readonly string[] _categories;
    private readonly List<TextBox> _paramLabelBoxes = new();

    /// <summary>Result after user saves; null if cancelled.</summary>
    public UserCommand? Result { get; private set; }

    /// <summary>
    /// Open for creating a new command.
    /// </summary>
    public CommandEditorDialog(string[] categoryNames, UserCommand? edit = null)
    {
        InitializeComponent();
        _categories = categoryNames;

        foreach (var cat in categoryNames)
            CbCategory.Items.Add(cat);

        if (edit != null)
        {
            WinTitle.Text = LocalizationService.Get("Custom.EditTitle");
            TbName.Text = edit.Name;
            TbDesc.Text = edit.Description;
            TbCommand.Text = edit.PowerShellCommand;
            CbCategory.SelectedItem = categoryNames.FirstOrDefault(c => c == edit.Category) ?? categoryNames[0];
            RefreshParameters();
        }
        else
        {
            CbCategory.SelectedItem = categoryNames[0];
        }
    }

    /// <summary>
    /// Parse {key} placeholders from the command template and generate parameter label fields.
    /// </summary>
    private void RefreshParameters()
    {
        ParametersPanel.Children.Clear();
        _paramLabelBoxes.Clear();

        var template = TbCommand.Text;
        if (string.IsNullOrWhiteSpace(template)) return;

        var matches = Regex.Matches(template, @"\{(\w+)\}");
        var seen = new HashSet<string>();

        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            if (!seen.Add(key)) continue;

            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            var label = new TextBlock
            {
                Text = $"{{{key}}}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6b, 0x6b, 0x70)),
                Margin = new Thickness(0, 0, 0, 3),
            };
            row.Children.Add(label);

            var tb = new TextBox
            {
                Text = key,
                FontSize = 12,
                Padding = new Thickness(8, 5, 8, 5),
                Background = new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf7)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xe4, 0xe4, 0xe7)),
                BorderThickness = new Thickness(1),
            };
            row.Children.Add(tb);
            _paramLabelBoxes.Add(tb);

            ParametersPanel.Children.Add(row);
        }

        TbParamTitle.Text = seen.Count > 0
            ? $"{LocalizationService.Get("Custom.ParamTitle")} ({seen.Count})"
            : LocalizationService.Get("Custom.ParamNone");
    }

    private void TbCommand_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshParameters();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = TbName.Text.Trim();
        var command = TbCommand.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(LocalizationService.Get("Custom.NameRequired"), LocalizationService.Get("Custom.Required"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(command))
        {
            MessageBox.Show(LocalizationService.Get("Custom.CmdRequired"), LocalizationService.Get("Custom.Required"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var parameters = new List<CommandParameter>();
        var matches = Regex.Matches(command, @"\{(\w+)\}");
        var seen = new HashSet<string>();
        int i = 0;

        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            if (!seen.Add(key)) continue;

            var label = i < _paramLabelBoxes.Count ? _paramLabelBoxes[i].Text.Trim() : key;
            if (string.IsNullOrWhiteSpace(label)) label = key;

            parameters.Add(new CommandParameter
            {
                Key = key,
                Label = label,
                DefaultValue = "",
                Required = true,
                Type = ParameterType.Text,
            });
            i++;
        }

        Result = new UserCommand
        {
            Name = name,
            Description = TbDesc.Text.Trim(),
            Category = CbCategory.SelectedItem?.ToString() ?? _categories[0],
            PowerShellCommand = command,
            Parameters = parameters,
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private void Close_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }
}
