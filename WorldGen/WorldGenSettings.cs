namespace Reroot.WorldGen;

public sealed class WorldGenSettings
{
    public WorldGenSettings()
    {
    }

    public int Seed { get; set; }

    public float Frequency { get; set; } = 0.12f;

    public int Octaves { get; set; } = 3;

    public int HeightLevels { get; set; } = 8;
}
