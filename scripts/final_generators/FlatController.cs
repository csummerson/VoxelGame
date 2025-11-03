using Godot;
using System;

public partial class FlatController : Node
{
    [Export] public Node3D playerNode;
    [Export] public int renderDistance = 8;
    [Export] public int simulationDistance = 8;
    [Export] public int[] lodThresholds = { 8, 16, 32, 64 };
    [Export] public int seed;
    [Export] public bool randomSeed = true;
    [Export] public int maxChunkThreads = 4;
    [Export] public bool useSurfaceNets = true;

    private FlatTerrainManager terrainManager;
    
    public override void _Ready()
    {
        seed = GameSettings.Instance.seed;
        lodThresholds[0] = GameSettings.Instance.viewDistance;
        maxChunkThreads = GameSettings.Instance.threadCount;
        
        terrainManager = new FlatTerrainManager(playerNode, lodThresholds, maxChunkThreads, seed, useSurfaceNets);
        terrainManager.Name = "Terrain Manager";
        AddChild(terrainManager);
    }


    public void UpdateSettings()
    {
        lodThresholds[0] = GameSettings.Instance.viewDistance;
        maxChunkThreads = GameSettings.Instance.threadCount;
        useSurfaceNets = GameSettings.Instance.surfaceNets;
        seed = GameSettings.Instance.seed;
        GameManager.Instance.worldSeed = seed.ToString();

        // just gonna crapily code this because im lazy rn fr fr
        terrainManager.lodThresholds[0] = GameSettings.Instance.viewDistance;
        terrainManager.maxChunkThreads = GameSettings.Instance.threadCount;

        if (GameSettings.Instance.dirty)
        {
            GameSettings.Instance.ScrubbyScrubby();
            Reset();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_reload"))
        {
            Reset();
        }
    }
    
    public void Reset()
    {
        FlatTerrainManager terrain = GetChild<FlatTerrainManager>(0);
        terrain.QueueFree();

        terrainManager = new FlatTerrainManager(playerNode, lodThresholds, maxChunkThreads, seed, useSurfaceNets);
        terrainManager.Name = "Terrain Manager";
        AddChild(terrainManager);
    }
}
