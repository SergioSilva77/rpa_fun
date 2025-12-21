using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeImageItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "shape.image";
    public string DisplayName => "Imagem";
    public string? Description => "Imagem (placeholder) conectável (arraste para a área de trabalho).";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 4;
    public Geometry? Icon => null;
    public string? IconText => "IMG";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho para criar uma imagem placeholder.");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Images.Add(new WorkspaceImageViewModel(
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            source: null,
            displayName: DisplayName));
    }
}

