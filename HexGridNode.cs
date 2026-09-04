using System.Collections.Generic;
using Godot;
using Reroot.Hex;
using Reroot.WorldGen;

namespace Reroot;

[Tool]
public partial class HexGridNode : Node3D
{
    [Export(PropertyHint.Range, "0, 64, 1")]
    public int MapRadius
    {
        get => _mapRadius;
        set { _mapRadius = Mathf.Max(0, value); QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "0.1, 4.0, 0.1")]
    public float TileRadius
    {
        get => _tileRadius;
        set { _tileRadius = Mathf.Max(0.1f, value); QueueRebuild(); }
    }

    [Export]
    public HexOrientation Orientation
    {
        get => _orientation;
        set { _orientation = value; QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "0.05, 2.0, 0.05")]
    public float HeightStep
    {
        get => _heightStep;
        set { _heightStep = Mathf.Max(0.01f, value); QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "0, 32, 1")]
    public int ChunkRadius
    {
        get => _chunkRadius;
        set { _chunkRadius = Mathf.Max(0, value); QueueRebuild(); }
    }

    [Export]
    public int Seed
    {
        get => _seed;
        set { _seed = value; QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "0.01, 1.0, 0.01")]
    public float NoiseFrequency
    {
        get => _noiseFrequency;
        set { _noiseFrequency = Mathf.Max(0.001f, value); QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "1, 8, 1")]
    public int NoiseOctaves
    {
        get => _noiseOctaves;
        set { _noiseOctaves = Mathf.Clamp(value, 1, 8); QueueRebuild(); }
    }

    [Export(PropertyHint.Range, "1, 16, 1")]
    public int HeightLevels
    {
        get => _heightLevels;
        set { _heightLevels = Mathf.Max(1, value); QueueRebuild(); }
    }

    private int _mapRadius = 8;
    private float _tileRadius = 1.0f;
    private HexOrientation _orientation = HexOrientation.Pointy;
    private float _heightStep = 0.25f;
    private int _chunkRadius = 4;
    private int _seed;
    private float _noiseFrequency = 0.12f;
    private int _noiseOctaves = 3;
    private int _heightLevels = 8;

    private bool _rebuildQueued;
    private readonly Dictionary<HexCoord, WorldTile> _tiles = new();
    private readonly List<MeshInstance3D> _chunks = new();
    private StandardMaterial3D? _material;

    public HexGrid? Grid { get; private set; }

    public HexLayout? Layout { get; private set; }

    public IReadOnlyDictionary<HexCoord, WorldTile> Tiles => _tiles;

    public override void _Ready() => Rebuild();

    public void Rebuild()
    {
        _rebuildQueued = false;

        var grid = HexGrid.Hexagonal(MapRadius);
        var layout = new HexLayout(Orientation, TileRadius);
        var settings = new WorldGenSettings
        {
            Seed = Seed,
            Frequency = NoiseFrequency,
            Octaves = NoiseOctaves,
            HeightLevels = HeightLevels,
        };

        var generator = new WorldGenerator(settings, ColorMap.Terrain());
        var tiles = generator.Generate(grid, layout);

        Grid = grid;
        Layout = layout;
        _tiles.Clear();
        foreach (var tile in tiles)
        {
            _tiles[tile.Key] = tile.Value;
        }

        BuildChunks(grid, layout);
    }

    public Vector3 GetTileTop(HexCoord coordinate)
    {
        if (Layout == null)
        {
            return Vector3.Zero;
        }

        var basePosition = Layout.ToWorld(coordinate);
        var height = _tiles.TryGetValue(coordinate, out WorldTile data)
            ? (data.HeightLevel + 1) * HeightStep
            : HeightStep;
        return new Vector3(basePosition.X, height, basePosition.Z);
    }

    private void BuildChunks(HexGrid grid, HexLayout layout)
    {
        ClearChunks();

        var chunkGroups = new Dictionary<HexChunk, List<HexCoord>>();
        foreach (var coordinate in grid)
        {
            var chunk = HexChunking.GetChunk(coordinate, ChunkRadius);
            if (!chunkGroups.TryGetValue(chunk, out var group))
            {
                chunkGroups[chunk] = group = [];
            }

            group.Add(coordinate);
        }

        var material = GetMaterial();
        foreach (var chunk in chunkGroups)
        {
            var mesh = HexMesher.Build(chunk.Value, _tiles, layout, HeightStep);
            if (mesh == null)
            {
                continue;
            }

            var instance = new MeshInstance3D
            {
                Name = $"Chunk_{chunk.Key.Center.Q}_{chunk.Key.Center.R}",
                Mesh = mesh,
                MaterialOverride = material,
            };

            AddChild(instance);
            AddChunkCollision(instance, mesh);
            _chunks.Add(instance);
        }
    }

    private static void AddChunkCollision(MeshInstance3D chunk, ArrayMesh mesh)
    {
        var body = new StaticBody3D
        {
            Name = "Collision",
        };

        var shape = new CollisionShape3D
        {
            Name = "Shape",
            Shape = mesh.CreateTrimeshShape(),
        };

        body.AddChild(shape);
        chunk.AddChild(body);
    }

    private void ClearChunks()
    {
        foreach (var chunk in _chunks)
        {
            if (IsInstanceValid(chunk))
            {
                chunk.QueueFree();
            }
        }

        _chunks.Clear();
    }

    private StandardMaterial3D GetMaterial()
    {
        return _material ??= new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Roughness = 0.9f,
        };
    }

    private void QueueRebuild()
    {
        if (!IsInsideTree() || _rebuildQueued)
        {
            return;
        }

        _rebuildQueued = true;
        CallDeferred(nameof(Rebuild));
    }
}
