using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class WorkspaceImageViewModel : ViewModelBase, IMovableWorkspaceSurface
{
    private bool _isSelected;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private ImageSource? _source;

    public WorkspaceImageViewModel(double x, double y, double width, double height, ImageSource? source = null, string displayName = "Imagem")
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
        _source = source;
        DisplayName = displayName;
    }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public double X
    {
        get => _x;
        private set
        {
            if (SetProperty(ref _x, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public double Y
    {
        get => _y;
        private set
        {
            if (SetProperty(ref _y, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public double Width
    {
        get => _width;
        private set
        {
            if (SetProperty(ref _width, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public double Height
    {
        get => _height;
        private set
        {
            if (SetProperty(ref _height, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public Rect Bounds => new(X, Y, Width, Height);

    public ImageSource? Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    public void MoveBy(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }
}

