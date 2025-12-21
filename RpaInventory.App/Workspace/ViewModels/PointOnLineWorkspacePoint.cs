using System.ComponentModel;
using System.Windows;
using RpaInventory.App.Inventory.ViewModels;
using RpaInventory.App.Workspace.Geometry;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class PointOnLineWorkspacePoint : ViewModelBase, IMovableWorkspacePoint
{
    private readonly LineViewModel _parentLine;
    private double _t;

    public PointOnLineWorkspacePoint(LineViewModel parentLine, double t)
    {
        _parentLine = parentLine;
        _t = Math.Clamp(t, 0, 1);

        _parentLine.EndpointsChanged += ParentLine_EndpointsChanged;
        SubscribeToEndpoints();
    }

    public LineViewModel ParentLine => _parentLine;

    public double T
    {
        get => _t;
        private set
        {
            var clamped = Math.Clamp(value, 0, 1);
            if (!SetProperty(ref _t, clamped))
                return;

            RaisePositionChanged();
        }
    }

    public double X => GetPosition().X;
    public double Y => GetPosition().Y;

    public void MoveTo(double x, double y)
    {
        var p = new Point(x, y);
        var projection = WorkspaceGeometry.ProjectPointOntoSegment(p, GetP1(), GetP2());
        T = projection.T;
    }

    private void ParentLine_EndpointsChanged(object? sender, EventArgs e)
    {
        SubscribeToEndpoints();
        RaisePositionChanged();
    }

    private void SubscribeToEndpoints()
    {
        _parentLine.P1.PropertyChanged -= Endpoint_PropertyChanged;
        _parentLine.P2.PropertyChanged -= Endpoint_PropertyChanged;

        _parentLine.P1.PropertyChanged += Endpoint_PropertyChanged;
        _parentLine.P2.PropertyChanged += Endpoint_PropertyChanged;
    }

    private void Endpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IWorkspacePoint.X) or nameof(IWorkspacePoint.Y))
            RaisePositionChanged();
    }

    private void RaisePositionChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
    }

    private Point GetPosition()
    {
        var p1 = GetP1();
        var p2 = GetP2();
        return p1 + ((p2 - p1) * _t);
    }

    private Point GetP1() => new(_parentLine.P1.X, _parentLine.P1.Y);
    private Point GetP2() => new(_parentLine.P2.X, _parentLine.P2.Y);
}

