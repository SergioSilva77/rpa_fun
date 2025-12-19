using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Database;

public sealed class DbSelectItem : IInventoryItem
{
    public string Id => "db.select";
    public string DisplayName => "Selecionar";
    public string? Description => "Executa um SELECT no banco de dados.";
    public InventorySectionId SectionId => InventorySectionId.Database;
    public int? SlotIndex => 2;
    public Geometry? Icon => null;
    public string? IconText => "SEL";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ação placeholder: SELECT (RPA).");
}

