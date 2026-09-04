using System;

namespace Reroot.Hex;

public readonly record struct HexChunk
{
    public HexCoord Center { get; }

    public int Radius { get; }

    public int TileCount => 1 + 3 * Radius * (Radius + 1);

    public HexChunk(HexCoord center, int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        Center = center;
        Radius = radius;
    }

    public bool Contains(HexCoord coordinate)
    {
        return Center.DistanceTo(coordinate) <= Radius;
    }
}
