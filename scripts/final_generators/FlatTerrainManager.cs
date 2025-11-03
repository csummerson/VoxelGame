using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public partial class FlatTerrainManager : Node
{
    private Node3D playerNode;
    private int seed;
    public int[] lodThresholds;
    public int maxChunkThreads = 4;
    private Dictionary<(Vector2I, int), ChunkManager> activeChunks = new();
    private PriorityQueue<ChunkManager, int> loadQueue = new();

    Stopwatch stopwatch;

    private int activeLoads = 0;
    TerrainField terrainField;

    bool useSurfaceNets;

    private float chunkUpdateTimer = 0f;
    private const float ChunkUpdateInterval = 0.5f;


    public FlatTerrainManager(Node3D playerNode, int[] lodThresholds, int maxChunkThreads, int seed, bool useSurfaceNets)
    {
        this.playerNode = playerNode;
        this.lodThresholds = lodThresholds;
        this.maxChunkThreads = maxChunkThreads;
        this.seed = seed;
        terrainField = new TerrainField(seed);
        this.useSurfaceNets = useSurfaceNets;
    }
    

    public override void _Process(double delta)
    {
        chunkUpdateTimer += (float)delta;
        if (chunkUpdateTimer >= ChunkUpdateInterval)
        {
            UpdateChunks();
            chunkUpdateTimer = 0f;
        }
        
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
                //GD.Print("Attemping to apply deform to chunks: " + chunk.Name);
            }
        }
    }
    
    private void UpdateChunks()
    {
        Vector3 playerPosition = playerNode.GlobalPosition;
        Vector2I playerChunk = WorldGenUtility.WorldToLod(playerPosition, 1);
        GameManager.Instance.chunkInfo = playerChunk.ToString();
        HashSet<(Vector2I, int)> needed = new();

        int innerRadius = 0;

        for (int i = 0; i < 1; i++)
        {
            int resolution = (int)Mathf.Pow(2, i);
            int outerRadius = innerRadius + lodThresholds[i];

            playerChunk = WorldGenUtility.WorldToLod(playerPosition, resolution);

            // uneeded for now
            //int xOffset = (playerChunk.X % 2 == 0) ? 1 : 0;
            //int yOffset = (playerChunk.Y % 2 == 0) ? 1 : 0;

            for (int dx = -outerRadius; dx <= outerRadius; dx++)
            {
                for (int dy = -outerRadius; dy <= outerRadius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) < innerRadius)
                        continue;

                    float distance = Mathf.Sqrt(dx * dx + dy * dy) + 0.1f;

                    Vector2I offset = new Vector2I(dx, dy);
                    // Place into the next chunks base
                    //offset = offset;

                    Vector2I chunkCoordinate = playerChunk + offset;
                    var key = (chunkCoordinate, resolution);
                    needed.Add(key);

                    if (!activeChunks.ContainsKey(key))
                    {
                        bool loadCollider = true;
                        ChunkManager chunk = new ChunkManager(resolution, loadCollider, useSurfaceNets);
                        chunk.Position = new Vector3(chunkCoordinate.X, 0, chunkCoordinate.Y) * WorldGenUtility.chunkSize * resolution;
                        chunk.Name = $"LOD{i}_{chunkCoordinate}";
                        AddChild(chunk);
                        activeChunks.Add(key, chunk);

                        // prioritize the lower regions before everything else
                        // maybe even move LOD stuff to a seperate thread depending on how expensive it ends up being
                        loadQueue.Enqueue(chunk, i * 1000 + (int)distance);
                    }
                }
            }

            innerRadius = (outerRadius / 2) + 1;
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
        if (activeLoads == 0 && loadQueue.Count > 0 && (stopwatch == null || !stopwatch.IsRunning))
            StartLoadingTimer();

        while (activeLoads < maxChunkThreads && loadQueue.Count > 0)
        {
            ChunkManager chunk = loadQueue.Dequeue();
            activeLoads++;

            chunk.OnChunkLoaded += HandleChunkLoaded;

            chunk.CreateChunkAsync(terrainField);
        }

        if (activeLoads == 0 && loadQueue.Count == 0)
            StopLoadingTimer();
    }

    private void HandleChunkLoaded(ChunkManager chunk)
    {
        activeLoads = Math.Max(0, activeLoads - 1);
        chunk.OnChunkLoaded -= HandleChunkLoaded;

        if (activeLoads == 0 && loadQueue.Count == 0)
            StopLoadingTimer();
    }

    private void StartLoadingTimer()
    {
        stopwatch = new Stopwatch();
        stopwatch.Start();
    }

    private void StopLoadingTimer()
    {
        if (stopwatch != null && stopwatch.IsRunning)
        {
            stopwatch.Stop();
            GD.Print($"All chunks loaded in: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
