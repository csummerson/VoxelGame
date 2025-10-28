using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class FlatTerrainManager : Node
{
    private Node3D playerNode;
    private int seed;
    private int[] lodThresholds;
    private int maxChunkThreads = 4;
    private Dictionary<(Vector2I, int), ChunkManager> activeChunks = new();
    private PriorityQueue<ChunkManager, int> loadQueue = new();

    private int activeLoads = 0;
    TerrainField terrainField;

    public FlatTerrainManager(Node3D playerNode, int[] lodThresholds, int maxChunkThreads, int seed)
    {
        this.playerNode = playerNode;
        this.lodThresholds = lodThresholds;
        this.maxChunkThreads = maxChunkThreads;
        this.seed = seed;
        terrainField = new TerrainField(seed);
    }

    public override void _Process(double delta)
    {
        UpdateChunks();
        ProcessQueue();
    }

    // For use with terrain deformation or other methods
    public bool TryGetChunk(Vector2I coord, out ChunkManager chunk, int resolution = 1)
    {
        return activeChunks.TryGetValue((coord, resolution), out chunk);
    }

    public void ApplyDeform(Vector3 point, int radius, float delta)
    {
        Vector2I baseChunk = WorldGenUtility.WorldToLod(point, 1);

        foreach (var offset in WorldGenUtility.chunkNeighbors)
        {
            Vector2I neighbor = baseChunk + offset;
            if (TryGetChunk(neighbor, out var chunk))
            {
                chunk.DeformLocal(point, radius, delta);
            }
        }
    }
    
    private void UpdateChunks()
    {
        
        Vector3 playerPosition = playerNode.GlobalPosition;
        Vector2I playerChunk = WorldGenUtility.WorldToLod(playerPosition, 1);
        GameManager.Instance.chunkInfo = playerChunk.ToString();
        HashSet<(Vector2I, int)> needed = new();

        for (int lodIndex = 0; lodIndex < lodThresholds.Length; lodIndex++)
        {
            int resolution = (int)Mathf.Pow(2, lodIndex);
            int outerRadius = lodThresholds[lodIndex] / resolution;
            int innerRadius = lodIndex == 0 ? 0 : lodThresholds[lodIndex - 1] / (int)Mathf.Pow(2, lodIndex - 1) - 1;

            playerChunk = WorldGenUtility.WorldToLod(playerPosition, resolution);

            int xOffset = (playerChunk.X % 2 == 0) ? 1 : 0;
            int yOffset = (playerChunk.Y % 2 == 0) ? 1 : 0;

            for (int dx = -outerRadius - (1 - xOffset); dx <= outerRadius + xOffset; dx++)
            {
                for (int dy = -outerRadius - (1 - yOffset); dy <= outerRadius + yOffset; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) < innerRadius)
                        continue;

                    Vector2I offset = new Vector2I(dx, dy);
                    Vector2I chunkCoordinate = playerChunk + offset;
                    var key = (chunkCoordinate, resolution);
                    needed.Add(key);

                    if (!activeChunks.ContainsKey(key))
                    {
                        bool loadCollider = lodIndex == 0;
                        ChunkManager chunk = new ChunkManager(resolution, loadCollider);
                        chunk.Position = new Vector3(chunkCoordinate.X, 0, chunkCoordinate.Y) * (WorldGenUtility.chunkSize - 1) * resolution;
                        chunk.Name = $"LOD{lodIndex}_{chunkCoordinate}";
                        AddChild(chunk);
                        activeChunks.Add(key, chunk);
                        
                        loadQueue.Enqueue(chunk, resolution);
                    }
                }
            }
        }

        var toRemove = new List<(Vector2I, int)>();
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

    private void ProcessQueue()
    {
        while (activeLoads < maxChunkThreads && loadQueue.Count > 0)
        {
            ChunkManager chunk = loadQueue.Dequeue();
            activeLoads++;

            chunk.OnChunkLoaded += HandleChunkLoaded;

            chunk.StartLoadingAsync(terrainField);
        }
    }

    private void HandleChunkLoaded(ChunkManager chunk)
    {
        activeLoads = Math.Max(0, activeLoads - 1);
        chunk.OnChunkLoaded -= HandleChunkLoaded;
    }
}
