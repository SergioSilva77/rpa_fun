using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Database;

public sealed class DbUpdateItem : IInventoryItem
{
    public string Id => "db.update";
    public string DisplayName => "Atualizar";
    public string? Description => "Executa um UPDATE no banco de dados.";
    public InventorySectionId SectionId => InventorySectionId.Database;
    public int? SlotIndex => 1;
    public Geometry? Icon => null;
    public string? IconText => "UPD";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ação placeholder: UPDATE (RPA).");
}

