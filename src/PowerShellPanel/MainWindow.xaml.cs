using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PowerShellPanel.ViewModels;
using PowerShellPanel.Views;

namespace PowerShellPanel;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = (MainViewModel)DataContext;

        // Wire terminal output to RichTextBox
        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.TerminalOutput))
            {
                UpdateTerminalDisplay(_vm.TerminalOutput);
            }
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Select first nav item on load
        if (NavListBox.Items.Count > 0)
            NavListBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Append terminal output to the RichTextBox, maintaining scroll position.
    /// </summary>
    private void UpdateTerminalDisplay(string fullText)
    {
        TerminalOutputBox.Document.Blocks.Clear();

        var paragraph = new Paragraph
        {
            Margin = new Thickness(0),
            LineHeight = 1.4,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0xd4, 0xd4, 0xd4)),
        };

        // Colorize the text: prompt lines green, errors red, normal text default
        foreach (var line in fullText.Split('\n'))
        {
            if (string.IsNullOrEmpty(line) && paragraph.Inlines.Count == 0)
                continue;

            Brush color = line switch
            {
                _ when line.StartsWith("PS>") => new SolidColorBrush(Color.FromRgb(0x6a, 0x99, 0x55)),
                _ when line.StartsWith("[Error]") => new SolidColorBrush(Color.FromRgb(0xf4, 0x87, 0x71)),
                _ when line.StartsWith("───") => new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
                _ => new SolidColorBrush(Color.FromRgb(0xd4, 0xd4, 0xd4)),
            };

            paragraph.Inlines.Add(new Run(line + "\n") { Foreground = color });
        }

        TerminalOutputBox.Document.Blocks.Add(paragraph);
        TerminalOutputBox.ScrollToEnd();
    }

    /// <summary>
    /// Manually scroll the RichTextBox's internal ScrollViewer on mouse wheel,
    /// since the custom ScrollViewer template loses default wheel handling.
    /// </summary>
    private void TerminalOutputBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(TerminalOutputBox);
        if (scrollViewer == null) return;

        // Scroll up = wheel positive (delta > 0), scroll down = wheel negative
        double offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
        offset = Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight));
        scrollViewer.ScrollToVerticalOffset(offset);
        e.Handled = true;
    }

    /// <summary>
    /// Recursively find the first child of a given type in the visual tree.
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                return typed;

            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }
        return null;
    }

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavListBox.SelectedIndex < 0) return;

        // Scroll the corresponding category into view in the content area
        var container = ContentItems.ItemContainerGenerator.ContainerFromIndex(NavListBox.SelectedIndex);
        if (container is FrameworkElement fe)
        {
            fe.BringIntoView();
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void ClearTerminal_Click(object sender, RoutedEventArgs e)
    {
        _vm.ClearTerminal();
        TerminalOutputBox.Document.Blocks.Clear();
    }
}
