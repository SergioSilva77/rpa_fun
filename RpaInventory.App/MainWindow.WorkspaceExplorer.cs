using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RpaInventory.App.WorkspaceExplorer;

namespace RpaInventory.App;

public partial class MainWindow
{
    private Point _workspaceExplorerDragStart;
    private WorkspaceTreeNodeViewModel? _workspaceExplorerDragNode;
    private WorkspaceFilePreviewWindow? _workspacePreviewWindow;

    private void WorkspaceNewButton_Click(object sender, RoutedEventArgs e)
    {
        var explorer = ViewModel?.WorkspaceExplorer;
        if (explorer is null)
            return;

        if (explorer.SelectedNode is null)
        {
            explorer.CreateProject();
            return;
        }

        var menu = new ContextMenu
        {
            DataContext = explorer.SelectedNode,
            PlacementTarget = (UIElement)sender,
        };

        if (explorer.SelectedNode.Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder)
        {
            menu.Items.Add(new MenuItem { Header = "Criar arquivo", Command = explorer.SelectedNode.CreateFileCommand });
            menu.Items.Add(new MenuItem { Header = "Criar pasta", Command = explorer.SelectedNode.CreateFolderCommand });
            menu.Items.Add(new Separator());
        }

        menu.Items.Add(new MenuItem { Header = "Renomear", Command = explorer.SelectedNode.RenameCommand });
        menu.Items.Add(new MenuItem { Header = "Deletar", Command = explorer.SelectedNode.DeleteCommand });

        menu.IsOpen = true;
    }

    private void WorkspaceExplorerTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _workspaceExplorerDragStart = e.GetPosition((IInputElement)sender);
        _workspaceExplorerDragNode = FindNodeFromEventSource(e.OriginalSource as DependencyObject);
    }

    private void WorkspaceExplorerTree_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _workspaceExplorerDragNode is null)
            return;

        var position = e.GetPosition((IInputElement)sender);
        var diff = position - _workspaceExplorerDragStart;
        if (Math.Abs(diff.X) < DragStartThresholdPixels && Math.Abs(diff.Y) < DragStartThresholdPixels)
            return;

        var data = new DataObject(typeof(WorkspaceTreeNodeViewModel), _workspaceExplorerDragNode);
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
    }

    private void WorkspaceExplorerTree_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetDragNode(e, out var dragged) || ViewModel?.WorkspaceExplorer is null)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = TryComputeDropDestination(e, dragged, out _, out _) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void WorkspaceExplorerTree_Drop(object sender, DragEventArgs e)
    {
        if (!TryGetDragNode(e, out var dragged))
            return;

        var explorer = ViewModel?.WorkspaceExplorer;
        if (explorer is null)
            return;

        if (!TryComputeDropDestination(e, dragged, out var destinationParent, out var destinationIndex))
            return;

        explorer.MoveNode(dragged, destinationParent, destinationIndex);
        e.Handled = true;
    }

    private void WorkspaceSearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count != 1)
            return;

        if (e.AddedItems[0] is not WorkspaceSearchResultViewModel result)
            return;

        var explorer = ViewModel?.WorkspaceExplorer;
        if (explorer is null)
            return;

        if (result.Kind is WorkspaceSearchResultKind.FileName or WorkspaceSearchResultKind.FolderName)
        {
            if (explorer.TrySelectNodeByPath(result.FullPath))
                explorer.ClearSearch();
            return;
        }

        if (result.Kind is not WorkspaceSearchResultKind.FileContent)
            return;

        _workspacePreviewWindow?.Close();
        _workspacePreviewWindow = new WorkspaceFilePreviewWindow(
        filePath: result.FullPath,
        lineNumber: result.LineNumber,
        columnIndex: result.ColumnIndex,
        matchLength: result.Match.Length)
        {
            Owner = this,
        };

        _workspacePreviewWindow.Show();
    }

    private static bool TryGetDragNode(DragEventArgs e, out WorkspaceTreeNodeViewModel node)
    {
        if (e.Data.GetDataPresent(typeof(WorkspaceTreeNodeViewModel)) &&
            e.Data.GetData(typeof(WorkspaceTreeNodeViewModel)) is WorkspaceTreeNodeViewModel vm)
        {
            node = vm;
            return true;
        }

        node = null!;
        return false;
    }

    private bool TryComputeDropDestination(
        DragEventArgs e,
        WorkspaceTreeNodeViewModel dragged,
        out WorkspaceTreeNodeViewModel? destinationParent,
        out int destinationIndex)
    {
        destinationParent = null;
        destinationIndex = -1;

        var explorer = ViewModel?.WorkspaceExplorer;
        if (explorer is null)
            return false;

        var targetNode = FindNodeFromEventSource(e.OriginalSource as DependencyObject);
        if (targetNode is null)
        {
            if (dragged.Kind != WorkspaceNodeKind.Project)
                return false;

            destinationParent = null;
            destinationIndex = explorer.Projects.Count;
            return true;
        }

        if (ReferenceEquals(targetNode, dragged))
            return false;

        if (dragged.Kind == WorkspaceNodeKind.Project && targetNode.Kind != WorkspaceNodeKind.Project)
            return false;

        var edgeRatio = 0.25;
        var originalSource = e.OriginalSource as DependencyObject;
        if (originalSource is null)
            return false;

        var targetItem = FindAncestor<TreeViewItem>(originalSource);
        if (targetItem is null)
            return false;

        var position = e.GetPosition(targetItem);
        var height = Math.Max(1.0, targetItem.ActualHeight);
        var isTopEdge = position.Y < height * edgeRatio;
        var isBottomEdge = position.Y > height * (1.0 - edgeRatio);

        var canDropInto = targetNode.Kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder;
        if (!isTopEdge && !isBottomEdge && canDropInto && dragged.Kind != WorkspaceNodeKind.Project)
        {
            destinationParent = targetNode;
            destinationIndex = targetNode.Children.Count;
            return true;
        }

        if (targetNode.Parent is null)
        {
            if (dragged.Kind == WorkspaceNodeKind.Project)
            {
                destinationParent = null;
                var targetIndex = explorer.Projects.IndexOf(targetNode);
                destinationIndex = targetIndex < 0 ? explorer.Projects.Count : targetIndex + (isBottomEdge ? 1 : 0);
            }
            else
            {
                // Arquivos/pastas não podem ficar no root: tratar como "dentro do projeto".
                destinationParent = targetNode;
                destinationIndex = isTopEdge ? 0 : targetNode.Children.Count;
            }
        }
        else
        {
            destinationParent = targetNode.Parent;
            var targetIndex = destinationParent.Children.IndexOf(targetNode);
            destinationIndex = targetIndex < 0 ? destinationParent.Children.Count : targetIndex + (isBottomEdge ? 1 : 0);
        }

        destinationIndex = AdjustIndexForSameParentMove(dragged, destinationParent, destinationIndex);
        return destinationIndex >= 0;
    }

    private int AdjustIndexForSameParentMove(
        WorkspaceTreeNodeViewModel dragged,
        WorkspaceTreeNodeViewModel? destinationParent,
        int destinationIndex)
    {
        var explorer = ViewModel?.WorkspaceExplorer;
        if (explorer is null)
            return destinationIndex;

        if (dragged.Kind == WorkspaceNodeKind.Project)
        {
            var oldIndex = explorer.Projects.IndexOf(dragged);
            if (oldIndex >= 0 && destinationIndex > oldIndex)
                destinationIndex--;
            return destinationIndex;
        }

        if (dragged.Parent is null || destinationParent is null)
            return destinationIndex;

        if (!ReferenceEquals(dragged.Parent, destinationParent))
            return destinationIndex;

        var oldChildIndex = destinationParent.Children.IndexOf(dragged);
        if (oldChildIndex >= 0 && destinationIndex > oldChildIndex)
            destinationIndex--;
        return destinationIndex;
    }

    private static WorkspaceTreeNodeViewModel? FindNodeFromEventSource(DependencyObject? source)
    {
        if (source is null)
            return null;

        return FindAncestor<TreeViewItem>(source)?.DataContext as WorkspaceTreeNodeViewModel;
    }
}
