using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.WorkspaceExplorer;

public sealed class WorkspaceSearchResultViewModel : ViewModelBase
{
    public WorkspaceSearchResultViewModel(
        WorkspaceSearchResultKind kind,
        string fullPath,
        string fileName,
        string before,
        string match,
        string after,
        int? lineNumber = null,
        int? columnIndex = null)
    {
        Kind = kind;
        FullPath = fullPath;
        FileName = fileName;
        Before = before;
        Match = match;
        After = after;
        LineNumber = lineNumber;
        ColumnIndex = columnIndex;
    }

    public WorkspaceSearchResultKind Kind { get; }
    public string FullPath { get; }
    public string FileName { get; }
    public string Before { get; }
    public string Match { get; }
    public string After { get; }
    public int? LineNumber { get; }
    public int? ColumnIndex { get; }

    public string DisplayText => $"{FileName} - {Before}{Match}{After}";
}

