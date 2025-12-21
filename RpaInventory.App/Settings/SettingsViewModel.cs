using System.ComponentModel;
using System.Runtime.CompilerServices;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Settings;

public sealed class SettingsViewModel : ViewModelBase
{
    private double _centerSnapThreshold = 10.0;
    private double _alignmentAngleThreshold = 5.0;
    private double _edgeSnapThreshold = 3.0;
    private double _snapThresholdPixels = 14.0;

    public double CenterSnapThreshold
    {
        get => _centerSnapThreshold;
        set => SetProperty(ref _centerSnapThreshold, value);
    }

    public double AlignmentAngleThreshold
    {
        get => _alignmentAngleThreshold;
        set => SetProperty(ref _alignmentAngleThreshold, value);
    }

    public double EdgeSnapThreshold
    {
        get => _edgeSnapThreshold;
        set => SetProperty(ref _edgeSnapThreshold, value);
    }

    public double SnapThresholdPixels
    {
        get => _snapThresholdPixels;
        set => SetProperty(ref _snapThresholdPixels, value);
    }

    public static SettingsViewModel Default { get; } = new();
}

