using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Database;

public sealed class DbDeleteItem : IInventoryItem
{
    public string Id => "db.delete";
    public string DisplayName => "Deletar";
    public string? Description => "Executa um DELETE no banco de dados.";
    public InventorySectionId SectionId => InventorySectionId.Database;
    public int? SlotIndex => 3;
    public Geometry? Icon => null;
    public string? IconText => "DEL";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ação placeholder: DELETE (RPA).");
}

