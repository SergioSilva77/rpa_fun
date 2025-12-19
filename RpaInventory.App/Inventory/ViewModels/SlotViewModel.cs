using RpaInventory.App.Inventory.Items;

namespace RpaInventory.App.Inventory.ViewModels;

public sealed class SlotViewModel : ViewModelBase
{
    private IInventoryItem? _item;

    public SlotViewModel(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public IInventoryItem? Item
    {
        get => _item;
        set
        {
            if (SetProperty(ref _item, value))
                OnPropertyChanged(nameof(HasItem));
        }
    }

    public bool HasItem => Item is not null;
}

