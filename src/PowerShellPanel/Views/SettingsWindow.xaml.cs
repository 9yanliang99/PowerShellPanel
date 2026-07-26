using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PowerShellPanel.Services;

namespace PowerShellPanel.Views;

public partial class SettingsWindow : Window
{
    private record LanguageOption(string Code, string Flag, string Name);

    private static readonly List<LanguageOption> Languages = new()
    {
        new("en",       "🇺🇸", "English"),
        new("zh-CN",    "🇨🇳", "简体中文"),
        new("zh-TW",    "🇹🇼", "繁體中文"),
    };

    public SettingsWindow()
    {
        InitializeComponent();

        // Populate language dropdown
        foreach (var lang in Languages)
            LanguageCombo.Items.Add(lang);

        // Select current
        var current = LocalizationService.Instance.CurrentLanguage;
        var selected = Languages.FirstOrDefault(l => l.Code == current)
                       ?? Languages[0];
        LanguageCombo.SelectedItem = selected;
    }

    private void LanguageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedItem is LanguageOption opt)
        {
            LocalizationService.Instance.CurrentLanguage = opt.Code;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }
}
