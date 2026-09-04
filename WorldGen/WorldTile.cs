using Godot;

namespace Reroot.WorldGen;

public readonly record struct WorldTile(int HeightLevel, float NormalizedHeight, Color Color);
