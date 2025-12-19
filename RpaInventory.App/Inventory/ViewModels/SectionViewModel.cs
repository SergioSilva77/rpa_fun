using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.ViewModels;

public sealed class SectionViewModel : ViewModelBase
{
    private bool _isSelected;

    public SectionViewModel(InventorySectionId id, string displayName, Geometry? icon, string? iconText)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
        IconText = iconText;
    }

    public InventorySectionId Id { get; }
    public string DisplayName { get; }
    public Geometry? Icon { get; }
    public string? IconText { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

