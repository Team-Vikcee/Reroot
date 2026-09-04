namespace Reroot.Hex;

public enum HexDirection
{
    East = 0,
    NorthEast = 1,
    NorthWest = 2,
    West = 3,
    SouthWest = 4,
    SouthEast = 5,
}

public static class HexDirectionExtensions
{
    private const int Count = 6;

    public static HexDirection Opposite(this HexDirection direction)
        => (HexDirection)(((int)direction + Count / 2) % Count);

    public static HexDirection Clockwise(this HexDirection direction)
        => (HexDirection)(((int)direction + 1) % Count);

    public static HexDirection CounterClockwise(this HexDirection direction)
        => (HexDirection)(((int)direction + Count - 1) % Count);
}
