using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeStartItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "shape.start";
    public string DisplayName => "START";
    public string? Description => "Ponto de início (verde). Use o botão START para executar.";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 7;
    public Geometry? Icon => null;
    public string? IconText => "STA";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho para criar o ponto START (verde).");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.Start,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

