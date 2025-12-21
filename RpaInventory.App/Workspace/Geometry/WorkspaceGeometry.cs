using System.Windows;

namespace RpaInventory.App.Workspace.Geometry;

public static class WorkspaceGeometry
{
    public static ProjectionResult ProjectPointOntoSegment(Point point, Point segmentA, Point segmentB)
    {
        var ab = segmentB - segmentA;
        var ap = point - segmentA;

        var abSquared = ab.X * ab.X + ab.Y * ab.Y;
        if (abSquared <= double.Epsilon)
            return new ProjectionResult(T: 0, Projection: segmentA, Distance: (point - segmentA).Length);

        var t = (ap.X * ab.X + ap.Y * ab.Y) / abSquared;
        t = Math.Clamp(t, 0, 1);

        var projection = segmentA + ab * t;
        var distance = (point - projection).Length;

        return new ProjectionResult(t, projection, distance);
    }
}

public readonly record struct ProjectionResult(double T, Point Projection, double Distance);
