using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items;

public interface IInventoryItem
{
    string Id { get; }
    string DisplayName { get; }
    string? Description { get; }
    InventorySectionId SectionId { get; }

    int? SlotIndex { get; }

    Geometry? Icon { get; }
    string? IconText { get; }

    void Execute(IExecutionContext context);
}

public interface IWorkspacePlaceableInventoryItem
{
    void PlaceOnWorkspace(RpaInventory.App.Workspace.ViewModels.WorkspaceViewModel workspace, System.Windows.Point position);
}
