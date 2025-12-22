namespace RpaInventory.App.WorkspaceExplorer;

public interface IWorkspaceDialogService
{
    void ShowInfo(string title, string message);
    void ShowError(string title, string message);
    bool Confirm(string title, string message);
    string? PromptText(string title, string message, string defaultValue);
}

