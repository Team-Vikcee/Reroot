using Godot;

namespace Reroot.Hex;

public enum HexOrientation
{
    Pointy,
    Flat,
}

public sealed class HexLayout
{
    private readonly struct Basis
    {
        public readonly float F0, F1, F2, F3;
        public readonly float B0, B1, B2, B3;
        public readonly float StartAngle;

        public Basis(
            float f0, float f1, float f2, float f3,
            float b0, float b1, float b2, float b3,
            float startAngle)
        {
            F0 = f0; F1 = f1; F2 = f2; F3 = f3;
            B0 = b0; B1 = b1; B2 = b2; B3 = b3;
            StartAngle = startAngle;
        }
    }

    private static readonly float Sqrt3 = Mathf.Sqrt(3.0f);

    private static readonly Basis PointyBasis = new(
        Sqrt3, Sqrt3 / 2.0f, 0.0f, 1.5f,
        Sqrt3 / 3.0f, -1.0f / 3.0f, 0.0f, 2.0f / 3.0f,
        0.5f);

    private static readonly Basis FlatBasis = new(
        1.5f, 0.0f, Sqrt3 / 2.0f, Sqrt3,
        2.0f / 3.0f, 0.0f, -1.0f / 3.0f, Sqrt3 / 3.0f,
        0.0f);

    private readonly Basis _basis;

    public HexOrientation Orientation { get; }

    public Vector2 Size { get; }

    public Vector3 Origin { get; }

    public HexLayout(HexOrientation orientation, Vector2 size, Vector3 origin = default)
    {
        Orientation = orientation;
        Size = size;
        Origin = origin;
        _basis = orientation == HexOrientation.Flat ? FlatBasis : PointyBasis;
    }

    public HexLayout(HexOrientation orientation, float radius, Vector3 origin = default)
        : this(orientation, new Vector2(radius, radius), origin)
    {
    }

    public Vector3 ToWorld(HexCoord hex)
    {
        var x = (_basis.F0 * hex.Q + _basis.F1 * hex.R) * Size.X;
        var z = (_basis.F2 * hex.Q + _basis.F3 * hex.R) * Size.Y;
        return new Vector3(x + Origin.X, Origin.Y, z + Origin.Z);
    }

    public HexCoord FromWorld(Vector3 world)
    {
        var px = (world.X - Origin.X) / Size.X;
        var pz = (world.Z - Origin.Z) / Size.Y;
        var q = _basis.B0 * px + _basis.B1 * pz;
        var r = _basis.B2 * px + _basis.B3 * pz;
        return Round(q, r);
    }

    public Vector3[] Corners(HexCoord hex)
    {
        var center = ToWorld(hex);
        var corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            var angle = Mathf.Pi * 2.0f * (_basis.StartAngle - i) / 6.0f;
            corners[i] = new Vector3(
                center.X + Size.X * Mathf.Cos(angle),
                center.Y,
                center.Z + Size.Y * Mathf.Sin(angle));
        }

        return corners;
    }

    private static HexCoord Round(float q, float r)
    {
        var s = -q - r;
        var roundedQ = Mathf.RoundToInt(q);
        var roundedR = Mathf.RoundToInt(r);
        var roundedS = Mathf.RoundToInt(s);

        var deltaQ = Mathf.Abs(roundedQ - q);
        var deltaR = Mathf.Abs(roundedR - r);
        var deltaS = Mathf.Abs(roundedS - s);

        if (deltaQ > deltaR && deltaQ > deltaS)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (deltaR > deltaS)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexCoord(roundedQ, roundedR);
    }
}
