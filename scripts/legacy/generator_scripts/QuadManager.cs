using Godot;
using System.Collections.Generic;

public partial class QuadManager : Node3D
{
    [Export] public Node3D Player;
    [Export] public float TileSize = 128f; // size of each quadtree tile
    [Export] public int MaxLevel = 4;      // max quadtree depth
    [Export] public float UpdateInterval = 0.5f;
    [Export] public int TileViewDistance = 1; // number of tiles around player

    [Export] public int ChunkResolution = 16; // number of vertices per chunk
    [Export] public float HeightScale = 20f;   // max terrain height
    [Export] public float NoiseScale = 0.05f;

    FastNoiseLite noise = new FastNoiseLite();
    private Dictionary<Vector2I, QuadtreeNode> activeTiles = new Dictionary<Vector2I, QuadtreeNode>();
    private float timeSinceUpdate = 0f;

    public override void _Ready()
    {
        // Initialize noise once
        noise.SetSeed(1337);       // optional for reproducibility
        noise.SetFrequency(1f);    // can adjust frequency to control hill spacing
    }

    public override void _Process(double delta)
    {
        if (Player == null) return;

        timeSinceUpdate += (float)delta;
        if (timeSinceUpdate >= UpdateInterval)
        {
            timeSinceUpdate = 0f;
            UpdateTiles();
        }
    }

    private void UpdateTiles()
    {
        Vector2 playerTile = new Vector2(
            Mathf.Floor(Player.GlobalPosition.X / TileSize),
            Mathf.Floor(Player.GlobalPosition.Z / TileSize)
        );

        HashSet<Vector2I> tilesToKeep = new HashSet<Vector2I>();

        for (int x = -TileViewDistance; x <= TileViewDistance; x++)
        {
            for (int y = -TileViewDistance; y <= TileViewDistance; y++)
            {
                Vector2I tileCoord = new Vector2I((int)playerTile.X + x, (int)playerTile.Y + y);
                tilesToKeep.Add(tileCoord);

                if (!activeTiles.ContainsKey(tileCoord))
                {
                    Vector2 tileWorldPos = (Vector2) tileCoord * TileSize;
                    var newTile = new QuadtreeNode(tileWorldPos + new Vector2(TileSize / 2, TileSize / 2), TileSize, 0);
                    newTile.SpawnChunk(this);
                    activeTiles[tileCoord] = newTile;
                }

                activeTiles[tileCoord].Update(Player.GlobalPosition, this, MaxLevel);
            }
        }

        // Unload distant tiles
        List<Vector2I> tilesToRemove = new List<Vector2I>();
        foreach (var kvp in activeTiles)
        {
            if (!tilesToKeep.Contains(kvp.Key))
            {
                kvp.Value.DestroyTile();
                tilesToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in tilesToRemove)
            activeTiles.Remove(key);
    }

    // Procedural terrain mesh generator
    public MeshInstance3D CreateChunkMesh(Vector2 position, float size, int level)
    {
        MeshInstance3D meshInstance = new MeshInstance3D();
        ArrayMesh mesh = new ArrayMesh();

        // Create vertices and indices
        var vertices = new Vector3[(ChunkResolution + 1) * (ChunkResolution + 1)];
        var indices = new int[ChunkResolution * ChunkResolution * 6];

        float step = size / ChunkResolution;

        // Noise offset based on position so tiles are seamless
        float worldOffsetX = position.X - size / 2;
        float worldOffsetY = position.Y - size / 2;

        // Generate vertices
        for (int y = 0; y <= ChunkResolution; y++)
        {
            for (int x = 0; x <= ChunkResolution; x++)
            {
                float vx = x * step;
                float vy = y * step;
                float noiseX = (worldOffsetX + vx) * NoiseScale;
                float noiseY = (worldOffsetY + vy) * NoiseScale;
                
                float height = noise.GetNoise2D(noiseX, noiseY) * HeightScale;

                vertices[y * (ChunkResolution + 1) + x] = new Vector3(vx + worldOffsetX, height, vy + worldOffsetY);
            }
        }

        // Generate indices
        int idx = 0;
        for (int y = 0; y < ChunkResolution; y++)
        {
            for (int x = 0; x < ChunkResolution; x++)
            {
                int i0 = y * (ChunkResolution + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (ChunkResolution + 1);
                int i3 = i2 + 1;

                indices[idx++] = i0;
                indices[idx++] = i1;
                indices[idx++] = i2;

                indices[idx++] = i1;
                indices[idx++] = i3;
                indices[idx++] = i2;
            }
        }

        var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max); // ensures 9 slots

            arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            arrays[(int)Mesh.ArrayType.Index] = indices;
            // leave other arrays empty (Normals, Tangents, Colors, UVs, etc.)

            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        meshInstance.Mesh = mesh;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.2f + 0.1f * level, 0.8f - 0.1f * level, 0.2f);
        meshInstance.MaterialOverride = mat;

        AddChild(meshInstance);
        return meshInstance;
    }
}
