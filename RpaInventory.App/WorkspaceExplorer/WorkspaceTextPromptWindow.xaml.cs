using System.Windows;

namespace RpaInventory.App.WorkspaceExplorer;

public partial class WorkspaceTextPromptWindow : Window
{
    public WorkspaceTextPromptWindow(string title, string message, string defaultValue)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ValueTextBox.Text = defaultValue ?? string.Empty;
        Loaded += (_, _) =>
        {
            ValueTextBox.Focus();
            ValueTextBox.SelectAll();
        };
    }

    public string Value => ValueTextBox.Text;

    private void Ok_Click(object sender, RoutedEventArgs e)
        => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}

