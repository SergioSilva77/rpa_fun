using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RpaInventory.App.Inventory.DragDrop;
using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Point _dragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Slot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _dragStartPoint = e.GetPosition(null);

    private void Slot_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentPosition = e.GetPosition(null);
        var diff = _dragStartPoint - currentPosition;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is not Border border)
            return;

        if (border.DataContext is not SlotViewModel slot)
            return;

        if (slot.Item is null)
            return;

        var data = new DataObject(DragDropFormats.InventoryItem, slot.Item);
        DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
    }

    private void WorkspaceDropTarget_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragDropFormats.InventoryItem) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void WorkspaceDropTarget_DragOver(object sender, DragEventArgs e)
        => WorkspaceDropTarget_DragEnter(sender, e);

    private void WorkspaceDropTarget_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DragDropFormats.InventoryItem))
            return;

        if (e.Data.GetData(DragDropFormats.InventoryItem) is not IInventoryItem item)
            return;

        if (DataContext is not MainViewModel vm)
            return;

        vm.AddToWorkspace(item);
        e.Handled = true;
    }
}
