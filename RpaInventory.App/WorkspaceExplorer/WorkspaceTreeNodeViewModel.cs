using System.Collections.ObjectModel;
using System.Windows.Input;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.WorkspaceExplorer;

public sealed class WorkspaceTreeNodeViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public WorkspaceTreeNodeViewModel(
        WorkspaceExplorerViewModel explorer,
        WorkspaceTreeNodeViewModel? parent,
        WorkspaceNodeKind kind,
        string name,
        string fullPath)
    {
        Explorer = explorer;
        Parent = parent;
        Kind = kind;
        Name = name;
        FullPath = fullPath;

        Children = new ObservableCollection<WorkspaceTreeNodeViewModel>();

        CreateFileCommand = new RelayCommand(
            execute: () => Explorer.CreateFile(this),
            canExecute: () => Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder);

        CreateFolderCommand = new RelayCommand(
            execute: () => Explorer.CreateFolder(this),
            canExecute: () => Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder);

        RenameCommand = new RelayCommand(
            execute: () => Explorer.Rename(this),
            canExecute: () => Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder or WorkspaceNodeKind.File);

        DeleteCommand = new RelayCommand(
            execute: () => Explorer.Delete(this),
            canExecute: () => Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder or WorkspaceNodeKind.File);
    }

    public WorkspaceExplorerViewModel Explorer { get; }
    public WorkspaceTreeNodeViewModel? Parent { get; }
    public WorkspaceNodeKind Kind { get; }
    public string Name { get; }
    public string FullPath { get; }
    public ObservableCollection<WorkspaceTreeNodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
                return;

            if (value)
                Explorer.SelectedNode = this;
        }
    }

    public ICommand CreateFileCommand { get; }
    public ICommand CreateFolderCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }
}

