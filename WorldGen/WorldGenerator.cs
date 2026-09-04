using System;
using System.Collections.Generic;
using Godot;
using Reroot.Hex;

namespace Reroot.WorldGen;

public sealed class WorldGenerator
{
    private readonly WorldGenSettings _settings;
    private readonly ColorMap _colorMap;

    public WorldGenerator(WorldGenSettings settings, ColorMap colorMap)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _colorMap = colorMap ?? throw new ArgumentNullException(nameof(colorMap));
    }

    public Dictionary<HexCoord, WorldTile> Generate(IEnumerable<HexCoord> coordinates, HexLayout layout)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        ArgumentNullException.ThrowIfNull(layout);

        var noise = CreateNoise();
        var levels = Mathf.Max(1, _settings.HeightLevels);

        var tiles = new Dictionary<HexCoord, WorldTile>();
        foreach (var coordinate in coordinates)
        {
            var world = layout.ToWorld(coordinate);

            var normalized = noise.GetNoise2D(world.X, world.Z) * 0.5f + 0.5f;
            normalized = Mathf.Clamp(normalized, 0.0f, 1.0f);

            var level = Mathf.Clamp(Mathf.FloorToInt(normalized * levels), 0, levels - 1);
            var color = _colorMap.Sample(normalized);

            tiles[coordinate] = new WorldTile(level, normalized, color);
        }

        return tiles;
    }

    private FastNoiseLite CreateNoise()
    {
        return new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Seed = _settings.Seed,
            Frequency = _settings.Frequency,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = Mathf.Max(1, _settings.Octaves),
        };
    }
}
