using System;
using System.Collections.Generic;

namespace Reroot.Hex;

public readonly record struct HexCoord(int Q, int R)
{
    public static readonly HexCoord Zero = new(0, 0);

    public int S => -Q - R;

    private static readonly HexCoord[] DirectionVectors =
    {
        new(1, 0),  // East
        new(1, -1), // NorthEast
        new(0, -1), // NorthWest
        new(-1, 0), // West
        new(-1, 1), // SouthWest
        new(0, 1),  // SouthEast
    };

    public static HexCoord Offset(HexDirection direction) => DirectionVectors[(int)direction];

    public HexCoord Neighbor(HexDirection direction) => this + Offset(direction);

    public IEnumerable<HexCoord> Neighbors()
    {
        foreach (var offset in DirectionVectors)
        {
            yield return this + offset;
        }
    }

    public int Length => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;

    public int DistanceTo(HexCoord other) => (this - other).Length;

    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);

    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);

    public static HexCoord operator *(HexCoord a, int factor) => new(a.Q * factor, a.R * factor);

    public override string ToString() => $"({Q}, {R}, {S})";
}
