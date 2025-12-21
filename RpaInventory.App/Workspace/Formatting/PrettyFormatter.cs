using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RpaInventory.App.Settings;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Workspace.Formatting;

public sealed class PrettyFormatter
{
    private readonly WorkspaceViewModel _workspace;
    private readonly SettingsViewModel _settings;

    public PrettyFormatter(WorkspaceViewModel workspace, SettingsViewModel settings)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void FormatSelection()
    {
        var selectedLines = _workspace.Lines.Where(l => l.IsSelected).ToList();
        var selectedShapes = _workspace.Shapes.Where(s => s.IsSelected).ToList();
        var selectedImages = _workspace.Images.Where(i => i.IsSelected).ToList();

        if (selectedLines.Count == 0 && selectedShapes.Count == 0 && selectedImages.Count == 0)
            return;

        // 1. Snap linhas próximas ao centro de formas
        SnapLinesToShapeCenters(selectedLines, selectedShapes, selectedImages);

        // 2. Alinhar linhas quase horizontais/verticais
        AlignLines(selectedLines);
    }

    private void SnapLinesToShapeCenters(
        IReadOnlyList<LineViewModel> lines,
        IReadOnlyList<WorkspaceShapeViewModel> shapes,
        IReadOnlyList<WorkspaceImageViewModel> images)
    {
        var allSurfaces = new List<IWorkspaceSurface>();
        allSurfaces.AddRange(shapes);
        allSurfaces.AddRange(images);

        foreach (var line in lines)
        {
            var p1 = new Point(line.P1.X, line.P1.Y);
            var p2 = new Point(line.P2.X, line.P2.Y);

            // Verificar P1
            var center1 = FindNearestShapeCenter(p1, allSurfaces);
            if (center1.HasValue && (p1 - center1.Value).Length <= _settings.CenterSnapThreshold)
            {
                if (line.P1 is FreeWorkspacePoint)
                {
                    var shape = FindShapeAtCenter(center1.Value, allSurfaces);
                    if (shape is not null)
                    {
                        var localX = center1.Value.X - shape.X;
                        var localY = center1.Value.Y - shape.Y;
                        line.P1 = new PointOnShapeWorkspacePoint(shape, localX, localY);
                    }
                }
            }

            // Verificar P2
            var center2 = FindNearestShapeCenter(p2, allSurfaces);
            if (center2.HasValue && (p2 - center2.Value).Length <= _settings.CenterSnapThreshold)
            {
                if (line.P2 is FreeWorkspacePoint)
                {
                    var shape = FindShapeAtCenter(center2.Value, allSurfaces);
                    if (shape is not null)
                    {
                        var localX = center2.Value.X - shape.X;
                        var localY = center2.Value.Y - shape.Y;
                        line.P2 = new PointOnShapeWorkspacePoint(shape, localX, localY);
                    }
                }
            }
        }
    }

    private Point? FindNearestShapeCenter(Point point, IReadOnlyList<IWorkspaceSurface> surfaces)
    {
        Point? nearest = null;
        var minDistance = double.MaxValue;

        foreach (var surface in surfaces)
        {
            var center = GetShapeCenter(surface);
            var distance = (point - center).Length;

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = center;
            }
        }

        return nearest;
    }

    private Point GetShapeCenter(IWorkspaceSurface surface)
    {
        return new Point(
            surface.X + (surface.Width / 2),
            surface.Y + (surface.Height / 2));
    }

    private IWorkspaceSurface? FindShapeAtCenter(Point center, IReadOnlyList<IWorkspaceSurface> surfaces)
    {
        foreach (var surface in surfaces)
        {
            var shapeCenter = GetShapeCenter(surface);
            if (Math.Abs(shapeCenter.X - center.X) < 0.1 && Math.Abs(shapeCenter.Y - center.Y) < 0.1)
                return surface;
        }

        return null;
    }

    private void AlignLines(IReadOnlyList<LineViewModel> lines)
    {
        // Coletar todas as linhas e formas conectadas
        var connectedItems = new HashSet<object>();
        var linesToProcess = new Queue<LineViewModel>(lines);

        while (linesToProcess.Count > 0)
        {
            var line = linesToProcess.Dequeue();
            if (connectedItems.Contains(line))
                continue;

            connectedItems.Add(line);

            // Adicionar formas conectadas
            if (line.P1 is PointOnShapeWorkspacePoint p1Shape)
            {
                connectedItems.Add(p1Shape.Shape);
                // Adicionar outras linhas conectadas a esta forma
                foreach (var otherLine in _workspace.Lines)
                {
                    if (!connectedItems.Contains(otherLine) &&
                        (otherLine.P1 is PointOnShapeWorkspacePoint op1 && ReferenceEquals(op1.Shape, p1Shape.Shape) ||
                         otherLine.P2 is PointOnShapeWorkspacePoint op2 && ReferenceEquals(op2.Shape, p1Shape.Shape)))
                    {
                        linesToProcess.Enqueue(otherLine);
                    }
                }
            }

            if (line.P2 is PointOnShapeWorkspacePoint p2Shape)
            {
                connectedItems.Add(p2Shape.Shape);
                // Adicionar outras linhas conectadas a esta forma
                foreach (var otherLine in _workspace.Lines)
                {
                    if (!connectedItems.Contains(otherLine) &&
                        (otherLine.P1 is PointOnShapeWorkspacePoint op1 && ReferenceEquals(op1.Shape, p2Shape.Shape) ||
                         otherLine.P2 is PointOnShapeWorkspacePoint op2 && ReferenceEquals(op2.Shape, p2Shape.Shape)))
                    {
                        linesToProcess.Enqueue(otherLine);
                    }
                }
            }
        }

        // Agora alinhar as linhas, movendo a linha inteira
        foreach (var line in lines)
        {
            var p1 = new Point(line.P1.X, line.P1.Y);
            var p2 = new Point(line.P2.X, line.P2.Y);

            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var angle = Math.Atan2(dy, dx) * (180.0 / Math.PI);

            // Normalizar ângulo para 0-360
            if (angle < 0) angle += 360;

            // Verificar se está quase horizontal (0° ou 180°)
            if (IsAlmostHorizontal(angle))
            {
                // Calcular Y médio desejado (onde ambos os pontos devem estar)
                var targetY = (p1.Y + p2.Y) / 2;
                
                // Calcular quanto cada ponto precisa se mover para chegar ao Y médio
                var deltaY1 = targetY - p1.Y;
                var deltaY2 = targetY - p2.Y;
                
                // Se os pontos não estão alinhados, mover a linha inteira
                if (Math.Abs(deltaY1) > 0.1 || Math.Abs(deltaY2) > 0.1)
                {
                    // Mover cada ponto individualmente para o Y médio, mantendo conexões
                    AlignPointToY(line.P1, targetY, connectedItems);
                    AlignPointToY(line.P2, targetY, connectedItems);
                }
            }
            // Verificar se está quase vertical (90° ou 270°)
            else if (IsAlmostVertical(angle))
            {
                // Calcular X médio desejado (onde ambos os pontos devem estar)
                var targetX = (p1.X + p2.X) / 2;
                
                // Calcular quanto cada ponto precisa se mover para chegar ao X médio
                var deltaX1 = targetX - p1.X;
                var deltaX2 = targetX - p2.X;
                
                // Se os pontos não estão alinhados, mover a linha inteira
                if (Math.Abs(deltaX1) > 0.1 || Math.Abs(deltaX2) > 0.1)
                {
                    // Mover cada ponto individualmente para o X médio, mantendo conexões
                    AlignPointToX(line.P1, targetX, connectedItems);
                    AlignPointToX(line.P2, targetX, connectedItems);
                }
            }
        }
    }

    private void AlignPointToY(IMovableWorkspacePoint point, double targetY, HashSet<object> connectedItems)
    {
        var currentY = point.Y;
        var deltaY = targetY - currentY;

        if (Math.Abs(deltaY) < 0.01)
            return;

        if (point is FreeWorkspacePoint freePoint)
        {
            freePoint.MoveTo(freePoint.X, targetY);
        }
        else if (point is PointOnShapeWorkspacePoint shapePoint && shapePoint.Shape is IMovableWorkspaceSurface movable)
        {
            // Mover a forma inteira para manter a conexão
            movable.MoveBy(0, deltaY);
        }
    }

    private void AlignPointToX(IMovableWorkspacePoint point, double targetX, HashSet<object> connectedItems)
    {
        var currentX = point.X;
        var deltaX = targetX - currentX;

        if (Math.Abs(deltaX) < 0.01)
            return;

        if (point is FreeWorkspacePoint freePoint)
        {
            freePoint.MoveTo(targetX, freePoint.Y);
        }
        else if (point is PointOnShapeWorkspacePoint shapePoint && shapePoint.Shape is IMovableWorkspaceSurface movable)
        {
            // Mover a forma inteira para manter a conexão
            movable.MoveBy(deltaX, 0);
        }
    }

    private bool IsAlmostHorizontal(double angle)
    {
        var normalized = angle % 180;
        return normalized <= _settings.AlignmentAngleThreshold || normalized >= (180 - _settings.AlignmentAngleThreshold);
    }

    private bool IsAlmostVertical(double angle)
    {
        var normalized = (angle - 90) % 180;
        if (normalized < 0) normalized += 180;
        return normalized <= _settings.AlignmentAngleThreshold || normalized >= (180 - _settings.AlignmentAngleThreshold);
    }
}

