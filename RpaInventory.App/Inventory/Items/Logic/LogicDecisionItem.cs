using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Logic;

public sealed class LogicDecisionItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "logic.decision";
    public string DisplayName => "Decisão";
    public string? Description => "Losango de lógica (TRUE/FALSE).";
    public InventorySectionId SectionId => InventorySectionId.Logic;
    public int? SlotIndex => 0;
    public Geometry? Icon => null;
    public string? IconText => "IF";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho para criar um losango de lógica.");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.LogicDecision,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

