using System;

namespace Reroot.Hex;

public static class HexChunking
{
    public static HexChunk GetChunk(HexCoord coordinate, int chunkRadius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(chunkRadius);

        if (chunkRadius == 0)
        {
            return new HexChunk(coordinate, chunkRadius);
        }

        var k = (long)chunkRadius;
        var determinant = 3 * k * k + 3 * k + 1;

        var mNumerator = (2 * k + 1) * coordinate.Q + k * coordinate.R;
        var nNumerator = -k * coordinate.Q + (k + 1) * coordinate.R;

        var m = mNumerator / (double)determinant;
        var n = nNumerator / (double)determinant;
        var (roundedM, roundedN) = RoundAxial(m, n);

        return new HexChunk(Center(roundedM, roundedN, k), chunkRadius);

        static HexCoord Center(long m, long n, long radius)
        {
            return new HexCoord(
                checked((int)(m * (radius + 1) - n * radius)),
                checked((int)(m * radius + n * (2 * radius + 1)))
            );
        }
    }

    public static HexCoord ChunkCenter(HexCoord coordinate, int chunkRadius)
    {
        return GetChunk(coordinate, chunkRadius).Center;
    }

    private static (long Q, long R) RoundAxial(double q, double r)
    {
        var s = -q - r;
        var roundedQ = (long)Math.Round(q, MidpointRounding.AwayFromZero);
        var roundedR = (long)Math.Round(r, MidpointRounding.AwayFromZero);
        var roundedS = (long)Math.Round(s, MidpointRounding.AwayFromZero);

        var qError = Math.Abs(roundedQ - q);
        var rError = Math.Abs(roundedR - r);
        var sError = Math.Abs(roundedS - s);

        if (qError > rError && qError > sError)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (rError > sError)
        {
            roundedR = -roundedQ - roundedS;
        }

        return (roundedQ, roundedR);
    }
}
