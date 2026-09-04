using System;
using Godot;

namespace Reroot.WorldGen;

public sealed class ColorMap
{
    private readonly Color[] _stops;

    public ColorMap(params Color[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("A color map needs at least one stop.", nameof(stops));
        }

        _stops = stops;
    }

    public Color Sample(float t)
    {
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        if (_stops.Length == 1)
        {
            return _stops[0];
        }

        var scaled = t * (_stops.Length - 1);
        var lower = Mathf.FloorToInt(scaled);
        if (lower >= _stops.Length - 1)
        {
            return _stops[^1];
        }

        var blend = scaled - lower;
        return _stops[lower].Lerp(_stops[lower + 1], blend);
    }

    public static ColorMap Terrain() => new(
        new Color(0.20f, 0.35f, 0.55f),
        new Color(0.28f, 0.48f, 0.65f),
        new Color(0.85f, 0.80f, 0.55f),
        new Color(0.45f, 0.66f, 0.36f),
        new Color(0.30f, 0.52f, 0.30f),
        new Color(0.55f, 0.50f, 0.35f),
        new Color(0.50f, 0.47f, 0.45f),
        new Color(0.92f, 0.94f, 0.96f));
}
