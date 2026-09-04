using System;
using System.Collections;
using System.Collections.Generic;

namespace Reroot.Hex;

public sealed class HexGrid : IReadOnlyCollection<HexCoord>
{
    private readonly HashSet<HexCoord> _coordinates;

    private HexGrid(HashSet<HexCoord> coordinates) => _coordinates = coordinates;

    public int Count => _coordinates.Count;

    public bool Contains(HexCoord coordinate) => _coordinates.Contains(coordinate);

    public IEnumerable<HexCoord> Neighbors(HexCoord coordinate)
    {
        foreach (var neighbor in coordinate.Neighbors())
        {
            if (_coordinates.Contains(neighbor))
            {
                yield return neighbor;
            }
        }
    }

    public static HexGrid Hexagonal(int radius)
    {
        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be non-negative.");
        }

        var coordinates = new HashSet<HexCoord>();
        for (int q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (int r = minR; r <= maxR; r++)
            {
                coordinates.Add(new HexCoord(q, r));
            }
        }

        return new HexGrid(coordinates);
    }

    public static HexGrid Rectangular(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        var coordinates = new HashSet<HexCoord>();
        for (int r = 0; r < height; r++)
        {
            var rowOffset = r >> 1;
            for (int q = -rowOffset; q < width - rowOffset; q++)
            {
                coordinates.Add(new HexCoord(q, r));
            }
        }

        return new HexGrid(coordinates);
    }

    public IEnumerator<HexCoord> GetEnumerator() => _coordinates.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}