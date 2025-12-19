using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Database;

public sealed class DbCallRoutineItem : IInventoryItem
{
    public string Id => "db.callRoutine";
    public string DisplayName => "Chamar Rotina";
    public string? Description => "Executa uma rotina/procedure no banco de dados.";
    public InventorySectionId SectionId => InventorySectionId.Database;
    public int? SlotIndex => 4;
    public Geometry? Icon => null;
    public string? IconText => "ROT";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ação placeholder: CALL ROUTINE (RPA).");
}

