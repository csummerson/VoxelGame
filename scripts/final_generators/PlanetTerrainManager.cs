using Godot;
using System;
using System.Collections.Generic;

public partial class PlanetTerrainManager : Node3D
{
    // Constructor fields
    private int chunkRadius;
    private FaceManager[] faces;
    private int[] lodThresholds;

    // Loading fields
    [Export] public Node3D playerNode;
    private Dictionary<(int, Vector2I, int), ChunkManager> activeChunks = new();

    // Wrapping fields
    private enum Face { Front = 0, Right = 1, Back = 2, Left = 3, Top = 4, Bottom = 5 }
    private enum Direction { Left, Right, Up, Down }
    record Neighbor(Face Face, CubeTransform Transform);
    Dictionary<(Face, Direction), Neighbor> neighbors;

    private struct CubeTransform
    {
        public bool Swap;     // Swap x/y
        public bool FlipX;    // Flip x (mirror)
        public bool FlipY;    // Flip y (mirror)
        public int OffsetX;   // Translation
        public int OffsetY;

        public (int, int) Apply(int x, int y, int bounds)
        {
            // Swap
            if (Swap) (x, y) = (y, x);

            // Flips
            if (FlipX) x = bounds - x - 1;
            if (FlipY) y = bounds - y - 1;

            // Offsets
            x += OffsetX;
            y += OffsetY;

            return (x, y);
        }
    }

    public PlanetTerrainManager(int chunkRadius, FaceManager[] faces, int[] lodThresholds)
    {
        this.chunkRadius = chunkRadius;
        this.faces = faces;
        this.lodThresholds = lodThresholds;
        AssembleNeighbors(chunkRadius * 2);
    }

    // Chunk loading logic
    public override void _Process(double delta)
    {
        Vector3 playerPosition = playerNode.GlobalPosition;
        int playerFace = WorldGenUtility.GetFaceIndex(playerPosition);
        Vector2I playerChunk = WorldGenUtility.SphereToFaceChunk(playerPosition, playerFace, chunkRadius);

        HashSet<(int, Vector2I, int)> needed = new();

        for (int r = 0; r < lodThresholds[0]; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;

                    Vector2I offset = new Vector2I(dx, dy);
                    Vector2I chunkToLoad = playerChunk + offset;
                    var coordinate = WrapFaceCoordinate(playerFace, chunkToLoad);

                    int resolution = WorldGenUtility.CalculateLOD(r, lodThresholds);

                    var key = (coordinate.Item1, coordinate.Item2, resolution);
                    needed.Add(key);

                    if (!activeChunks.ContainsKey(key))
                    {
                        bool loadCollider = r <= lodThresholds[0];
                        //ChunkManager chunk = faces[key.Item1].LoadChunkAsync(coordinate.Item2, resolution, loadCollider);
                        //activeChunks.Add(key, chunk);
                    }
                }
            }
        }

        var toRemove = new List<(int, Vector2I, int)>();
        foreach (var kvp in activeChunks)
        {
            if (!needed.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            activeChunks[key].QueueFree();
            activeChunks.Remove(key);
        }

    }

    // Chunk wrapping logic
    private (int, Vector2I) WrapFaceCoordinate(int ogFace, Vector2I coord)
    {
        int face = ogFace;
        int x = coord.X;
        int y = coord.Y;

        int bounds = chunkRadius * 2;

        while (x < 0 || x >= bounds || y < 0 || y >= bounds)
        {
            Direction dir;
            if (x < 0) dir = Direction.Left;
            else if (x >= bounds) dir = Direction.Right;
            else if (y < 0) dir = Direction.Down;
            else if (y >= bounds) dir = Direction.Up;
            else break; // already in bounds

            if (neighbors.TryGetValue(((Face)face, dir), out var neighbor))
            {
                (x, y) = neighbor.Transform.Apply(x, y, bounds);
                face = (int)neighbor.Face;
            }
            else
            {
                x = Mathf.Clamp(x, 0, bounds - 1);
                y = Mathf.Clamp(y, 0, bounds - 1);
                break;
            }
        }

        return (face, new Vector2I(x, y));
    }

    private void AssembleNeighbors(int bounds)
    {
        neighbors = new()
        {
            // Front face
            { (Face.Front, Direction.Left),  new Neighbor(Face.Left,  new CubeTransform { OffsetX = +bounds }) },
            { (Face.Front, Direction.Right), new Neighbor(Face.Right, new CubeTransform { OffsetX = -bounds }) },
            { (Face.Front, Direction.Up),    new Neighbor(Face.Top,   new CubeTransform { OffsetY = -bounds }) },
            { (Face.Front, Direction.Down), new Neighbor(Face.Bottom, new CubeTransform { OffsetY = +bounds }) },

            // Right face
            { (Face.Right, Direction.Left), new Neighbor(Face.Front, new CubeTransform {OffsetX = +bounds})},
            { (Face.Right, Direction.Right), new Neighbor(Face.Back, new CubeTransform {OffsetX = -bounds})},
            { (Face.Right, Direction.Up), new Neighbor(Face.Top, new CubeTransform {Swap = true, FlipX = true, OffsetX = +bounds})},
            { (Face.Right, Direction.Down), new Neighbor(Face.Bottom, new CubeTransform {Swap = true, FlipY = true, OffsetX = +bounds })},

            // Back face
            { (Face.Back, Direction.Left), new Neighbor(Face.Right, new CubeTransform {OffsetX = +bounds})},
            { (Face.Back, Direction.Right), new Neighbor(Face.Left, new CubeTransform {OffsetX = -bounds})},
            { (Face.Back, Direction.Up), new Neighbor(Face.Top, new CubeTransform {FlipX = true, FlipY = true, OffsetY = +bounds})},
            { (Face.Back, Direction.Down), new Neighbor(Face.Bottom, new CubeTransform {FlipX = true, FlipY = true, OffsetY = -bounds })},

            // Left face
            { (Face.Left, Direction.Left), new Neighbor(Face.Back, new CubeTransform {OffsetX = +bounds})},
            { (Face.Left, Direction.Right), new Neighbor(Face.Front, new CubeTransform {OffsetX = -bounds})},
            { (Face.Left, Direction.Up), new Neighbor(Face.Top, new CubeTransform {Swap = true, FlipY = true, OffsetX = -bounds})},
            { (Face.Left, Direction.Down), new Neighbor(Face.Bottom, new CubeTransform {Swap = true, FlipX = true, OffsetX = -bounds })},

            // Top face
            { (Face.Top, Direction.Left), new Neighbor(Face.Left, new CubeTransform {Swap = true, FlipX = true, OffsetY = +bounds})},
            { (Face.Top, Direction.Right), new Neighbor(Face.Right, new CubeTransform {Swap = true, FlipY = true, OffsetY = +bounds})},
            { (Face.Top, Direction.Up), new Neighbor(Face.Back, new CubeTransform { FlipX = true, FlipY = true, OffsetY = +bounds})},
            { (Face.Top, Direction.Down), new Neighbor(Face.Front, new CubeTransform { OffsetY = +bounds })},

            // Bottom face
            { (Face.Bottom, Direction.Left),  new Neighbor(Face.Left,  new CubeTransform { Swap = true, FlipY = true, OffsetY = -bounds }) },
            { (Face.Bottom, Direction.Right), new Neighbor(Face.Right, new CubeTransform { Swap = true, FlipX = true, OffsetY = -bounds }) },
            { (Face.Bottom, Direction.Up),    new Neighbor(Face.Front, new CubeTransform { OffsetY = -bounds }) },
            { (Face.Bottom, Direction.Down),  new Neighbor(Face.Back,  new CubeTransform { FlipX = true, FlipY = true, OffsetY = -bounds }) },
        };
    }
}
