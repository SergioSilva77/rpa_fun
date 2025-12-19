using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Database;

public sealed class DbInsertItem : IInventoryItem
{
    public string Id => "db.insert";
    public string DisplayName => "Inserir";
    public string? Description => "Executa um INSERT no banco de dados.";
    public InventorySectionId SectionId => InventorySectionId.Database;
    public int? SlotIndex => 0;
    public Geometry? Icon => null;
    public string? IconText => "INS";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ação placeholder: INSERT (RPA).");
}

