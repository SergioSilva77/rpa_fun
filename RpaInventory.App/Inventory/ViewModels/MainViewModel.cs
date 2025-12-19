using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using RpaInventory.App.Inventory.Catalog;
using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public const int SlotColumns = 7;
    public const int SlotRows = 30;
    public const int SlotCount = SlotColumns * SlotRows;

    private readonly IInventoryCatalog _catalog;
    private InventorySectionId _selectedSectionId;

    public MainViewModel(IInventoryCatalog catalog)
    {
        _catalog = catalog;

        TopSections = new ObservableCollection<SectionViewModel>(
            catalog.TopSections.Select(section => new SectionViewModel(section.Id, section.DisplayName, section.Icon, section.IconText)));
        BottomSections = new ObservableCollection<SectionViewModel>(
            catalog.BottomSections.Select(section => new SectionViewModel(section.Id, section.DisplayName, section.Icon, section.IconText)));

        Slots = new ObservableCollection<SlotViewModel>(Enumerable.Range(0, SlotCount).Select(index => new SlotViewModel(index)));
        WorkspaceItems = new ObservableCollection<IInventoryItem>();
        WorkspaceItems.CollectionChanged += WorkspaceItems_CollectionChanged;

        SelectSectionCommand = new RelayCommand<SectionViewModel>(SelectSection);

        SelectSection(TopSections.First());
    }

    public ObservableCollection<SectionViewModel> TopSections { get; }
    public ObservableCollection<SectionViewModel> BottomSections { get; }
    public ObservableCollection<SlotViewModel> Slots { get; }
    public ObservableCollection<IInventoryItem> WorkspaceItems { get; }

    public ICommand SelectSectionCommand { get; }

    public InventorySectionId SelectedSectionId
    {
        get => _selectedSectionId;
        private set => SetProperty(ref _selectedSectionId, value);
    }

    public bool IsWorkspaceEmpty => WorkspaceItems.Count == 0;

    public void AddToWorkspace(IInventoryItem item)
    {
        WorkspaceItems.Add(item);
    }

    private void SelectSection(SectionViewModel section)
    {
        SelectedSectionId = section.Id;

        foreach (var sectionVm in TopSections)
            sectionVm.IsSelected = sectionVm.Id == section.Id;

        foreach (var sectionVm in BottomSections)
            sectionVm.IsSelected = sectionVm.Id == section.Id;

        LoadSlotsForSection(section.Id);
    }

    private void LoadSlotsForSection(InventorySectionId sectionId)
    {
        foreach (var slot in Slots)
            slot.Item = null;

        var items = _catalog.GetItems(sectionId);
        ApplyItemsToSlots(items);
    }

    private void ApplyItemsToSlots(IReadOnlyList<IInventoryItem> items)
    {
        if (items.Count == 0)
            return;

        var occupied = new bool[SlotCount];

        foreach (var item in items)
        {
            var assigned = TryAssignPreferredSlot(item, occupied);
            if (assigned)
                continue;

            var free = FindNextFreeSlot(occupied);
            if (free < 0)
                break;

            Slots[free].Item = item;
            occupied[free] = true;
        }
    }

    private bool TryAssignPreferredSlot(IInventoryItem item, bool[] occupied)
    {
        if (item.SlotIndex is not int slotIndex)
            return false;

        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        if (occupied[slotIndex])
            return false;

        Slots[slotIndex].Item = item;
        occupied[slotIndex] = true;
        return true;
    }

    private static int FindNextFreeSlot(bool[] occupied)
    {
        for (var index = 0; index < occupied.Length; index++)
        {
            if (!occupied[index])
                return index;
        }

        return -1;
    }

    private void WorkspaceItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(IsWorkspaceEmpty));
}
