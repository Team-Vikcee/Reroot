using System;
using System.Collections.Generic;
using Godot;
using Reroot.Hex;
using Reroot.WorldGen;

namespace Reroot;

public static class HexMesher
{
    private const float WallDarken = 0.15f;
    private const float HeightEpsilon = 0.0001f;

    public static ArrayMesh? Build(
        IEnumerable<HexCoord> chunkCoordinates,
        IReadOnlyDictionary<HexCoord, WorldTile> tiles,
        HexLayout layout,
        float heightStep)
    {
        ArgumentNullException.ThrowIfNull(chunkCoordinates);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(layout);

        var origin = layout.ToWorld(HexCoord.Zero);
        var directions = new (Vector2 Dir, HexCoord Offset)[6];
        for (int d = 0; d < 6; d++)
        {
            var offset = HexCoord.Offset((HexDirection)d);
            var world = layout.ToWorld(offset) - origin;
            directions[d] = (new Vector2(world.X, world.Z).Normalized(), offset);
        }

        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        var wroteAnything = false;

        foreach (var coordinate in chunkCoordinates)
        {
            if (!tiles.TryGetValue(coordinate, out WorldTile tile))
            {
                continue;
            }

            var top = TopOf(tile, heightStep);
            var center = layout.ToWorld(coordinate);
            var corners = layout.Corners(coordinate);

            AddTopFace(surface, center, corners, top, tile.Color);
            wroteAnything = true;

            for (int i = 0; i < 6; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % 6];

                var neighbor = coordinate + NeighborForEdge(a, b, center, directions);
                var neighborTop = tiles.TryGetValue(neighbor, out WorldTile neighborTile)
                    ? TopOf(neighborTile, heightStep)
                    : 0.0f;

                var bottom = Mathf.Clamp(neighborTop, 0.0f, top);
                if (top - bottom > HeightEpsilon)
                {
                    AddWall(surface, a, b, top, bottom, tile.Color.Darkened(WallDarken));
                }
            }
        }

        return wroteAnything ? surface.Commit() : null;
    }

    private static float TopOf(WorldTile tile, float heightStep) => (tile.HeightLevel + 1) * heightStep;

    private static HexCoord NeighborForEdge(Vector3 a, Vector3 b, Vector3 center, (Vector2 Dir, HexCoord Offset)[] directions)
    {
        var normal = new Vector2((a.X + b.X) * 0.5f - center.X, (a.Z + b.Z) * 0.5f - center.Z).Normalized();

        var best = 0;
        var bestDot = float.NegativeInfinity;
        for (int d = 0; d < directions.Length; d++)
        {
            var dot = normal.Dot(directions[d].Dir);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = d;
            }
        }

        return directions[best].Offset;
    }

    private static void AddTopFace(SurfaceTool surface, Vector3 center, Vector3[] corners, float top, Color color)
    {
        var centerTop = new Vector3(center.X, top, center.Z);
        for (int i = 0; i < 6; i++)
        {
            var a = new Vector3(corners[i].X, top, corners[i].Z);
            var b = new Vector3(corners[(i + 1) % 6].X, top, corners[(i + 1) % 6].Z);
            AddTriangle(surface, centerTop, b, a, Vector3.Up, color);
        }
    }

    private static void AddWall(SurfaceTool surface, Vector3 a, Vector3 b, float top, float bottom, Color color)
    {
        var aTop = new Vector3(a.X, top, a.Z);
        var bTop = new Vector3(b.X, top, b.Z);
        var aBottom = new Vector3(a.X, bottom, a.Z);
        var bBottom = new Vector3(b.X, bottom, b.Z);

        var normal = new Vector3(aTop.Z - bTop.Z, 0.0f, bTop.X - aTop.X).Normalized();

        AddTriangle(surface, aTop, bTop, aBottom, normal, color);
        AddTriangle(surface, bTop, bBottom, aBottom, normal, color);
    }

    private static void AddTriangle(SurfaceTool surface, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal, Color color)
    {
        surface.SetColor(color);
        surface.SetNormal(normal);
        surface.AddVertex(v0);
        surface.SetColor(color);
        surface.SetNormal(normal);
        surface.AddVertex(v1);
        surface.SetColor(color);
        surface.SetNormal(normal);
        surface.AddVertex(v2);
    }
}
