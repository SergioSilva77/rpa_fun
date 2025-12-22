using System.Windows;

namespace RpaInventory.App.WorkspaceExplorer;

public sealed class WorkspaceDialogService : IWorkspaceDialogService
{
    public void ShowInfo(string title, string message)
        => MessageBox.Show(Application.Current?.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string title, string message)
        => MessageBox.Show(Application.Current?.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string title, string message)
        => MessageBox.Show(Application.Current?.MainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public string? PromptText(string title, string message, string defaultValue)
    {
        var prompt = new WorkspaceTextPromptWindow(title, message, defaultValue)
        {
            Owner = Application.Current?.MainWindow,
        };

        var result = prompt.ShowDialog();
        return result == true ? prompt.Value : null;
    }
}

