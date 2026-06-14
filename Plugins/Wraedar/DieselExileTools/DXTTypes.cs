using System.Drawing;
using System.Numerics;

namespace DieselExileTools;

public sealed class DXTSettings
{
    public bool LargeMapCenterFix;
    public bool ShowTools;
    public float MapScaleModifier = 0.187812f;
}

public readonly struct DXTRect
{
    public Vector2 TopLeft { get; }
    public Vector2 BottomRight { get; }

    public float Left => TopLeft.X;
    public float Top => TopLeft.Y;
    public float Right => BottomRight.X;
    public float Bottom => BottomRight.Y;
    public float Width => BottomRight.X - TopLeft.X;
    public float Height => BottomRight.Y - TopLeft.Y;

    public DXTRect(Vector2 topLeft, Vector2 bottomRight)
    {
        TopLeft = topLeft;
        BottomRight = bottomRight;
    }

    public DXTRect(Vector2 topLeft, float width, float height)
        : this(topLeft, topLeft + new Vector2(width, height))
    {
    }

    public DXTRect Expand(float padding)
    {
        var pad = new Vector2(padding, padding);
        return new DXTRect(TopLeft - pad, BottomRight + pad);
    }

    public bool Contains(Vector2 point)
        => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}

public readonly struct DXTVector2i : IEquatable<DXTVector2i>
{
    public int X { get; }
    public int Y { get; }

    public DXTVector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static DXTVector2i operator +(DXTVector2i a, DXTVector2i b) => new(a.X + b.X, a.Y + b.Y);
    public static DXTVector2i operator -(DXTVector2i a, DXTVector2i b) => new(a.X - b.X, a.Y - b.Y);
    public static bool operator ==(DXTVector2i a, DXTVector2i b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(DXTVector2i a, DXTVector2i b) => !(a == b);

    public float DistanceF(DXTVector2i other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public bool Equals(DXTVector2i other) => this == other;
    public override bool Equals(object? obj) => obj is DXTVector2i other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}
