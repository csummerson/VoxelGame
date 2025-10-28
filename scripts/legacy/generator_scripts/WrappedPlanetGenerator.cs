using Godot;
using System;
using System.Collections.Generic;

public partial class WrappedPlanetGenerator : Node3D
{
    public String chunkCoordinateString = "";

    [Export] PlanetSettings settings;
    int chunkRadius;
    int waterLevel = 1;
    WrappedFace[] faces = new WrappedFace[6];
    Vector3[] directions = { Vector3.Back, Vector3.Right, Vector3.Forward, Vector3.Left, Vector3.Up, Vector3.Down };
    int bounds = 0;
    int renderDistance = 16;
    int simulationDistance = 8;

    Dictionary<(int, int, int, int), WrappedChunk> activeChunks = new();

    public override void _Ready()
    {
        chunkRadius = settings.chunkRadius;
        renderDistance = settings.renderDistance;
        simulationDistance = settings.simulationDistance;
        bounds = chunkRadius * 2;

        for (int i = 0; i < directions.Length; i++)
        {
            WrappedFace face = new WrappedFace(chunkRadius, waterLevel, directions[i], i);
            face.Name = i.ToString();
            AddChild(face);
            faces[i] = face;
            //face.GenerateAllChunks();
        }

        AssembleNeighbors();
    }

    public override void _Process(double delta)
    {
        //return;

        Camera3D cam = GetViewport().GetCamera3D();
        Vector3 playerPosition = cam.GlobalPosition;
        int cubeFace = GetFace(playerPosition);

        Vector2 cubeCoordinate = WorldGenUtilities.SphereToFace(playerPosition, cubeFace, chunkRadius);
        Vector2I chunkCoordinate = (Vector2I) cubeCoordinate;

        // chunk loading logic
        HashSet<(int, int, int, int)> needed = new();
        //int bounds = chunkRadius * 2; // 0 to bounds is size of face chunk array
        
        chunkCoordinateString = $"Face: {cubeFace}, Chunk: {chunkCoordinate}";

        for (int r = 0; r <= renderDistance; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // Only process positions at this ring
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                        continue;

                    Vector2I chunkToLoad = chunkCoordinate + new Vector2I(dx, dy);
                    //int res = CalculateResolution(r);
                    int res = 1;
                    var coord = WrapFaceCoordinate(cubeFace, chunkToLoad);
                    var key = (coord.Item1, coord.Item2, coord.Item3, res);

                    needed.Add(key);

                    if (!activeChunks.ContainsKey(key))
                    {
                        //WrappedChunk chunk = faces[key.Item1].LoadChunkAsync(new Vector2I(key.Item2, key.Item3));
                        WrappedChunk chunk = faces[key.Item1].LoadChunkAsync(new Vector2I(key.Item2, key.Item3), res, true);
                        activeChunks.Add(key, chunk);
                    }
                }
            }
        }

        // cleanup
        var toRemove = new List<(int, int, int, int)>();
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

    private int CalculateResolution(int r)
    {
        int maxLOD = 4;
        float growth = 1.5f;

        if (r <= 0f)
            return 1;

        r = Mathf.Min(r, renderDistance);

        float baseRadius = renderDistance / Mathf.Pow(2, maxLOD - 1);

        int lodLevel = 1;
        float currentRadius = baseRadius;

        while (lodLevel < maxLOD && r > currentRadius)
        {
            currentRadius *= 2f;
            lodLevel++;
        }

        int resolution = (int)Mathf.Pow(2, lodLevel - 1);

        return resolution;
    }

    private int GetFace(Vector3 position)
    {
        Vector3 absolute = position.Abs();
        Vector3.Axis maxAxis = absolute.MaxAxisIndex();
        int axisSign = Mathf.Sign(position[(int)maxAxis]);

        return (maxAxis, axisSign) switch
        {
            (Vector3.Axis.Z, 1) => 0,
            (Vector3.Axis.X, 1) => 1,
            (Vector3.Axis.Z, -1) => 2,
            (Vector3.Axis.X, -1) => 3,
            (Vector3.Axis.Y, 1) => 4,
            (Vector3.Axis.Y, -1) => 5,
            _ => 0
        };
    }
    
    enum Face { Front = 0, Right = 1, Back = 2, Left = 3, Top = 4, Bottom = 5 }
    enum Direction { Left, Right, Up, Down }

    record Neighbor(Face Face, CubeTransform Transform);

    Dictionary<(Face, Direction), Neighbor> neighbors;

    struct CubeTransform
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
   

    private void AssembleNeighbors()
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

    private (int, int, int) WrapFaceCoordinate(int ogFace, Vector2I coord)
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

            if (neighbors.TryGetValue(( (Face)face, dir), out var neighbor))
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

        return (face, x, y);
    }
}
