using Godot;
using System;

public partial class FlatController : Node
{
    [Export] public Node3D playerNode;
    [Export] public int renderDistance = 8;
    [Export] public int simulationDistance = 8;
    [Export] public int[] lodThresholds = { 8, 16, 32, 64 };
    [Export] public int seed = 3564;
    [Export] public bool randomSeed = true;

    [Export] public int maxChunkThreads = 4;

    private FlatTerrainManager terrainManager;

    public override void _EnterTree()
    {
        if (randomSeed)
        {
            RandomNumberGenerator rng = new RandomNumberGenerator();
            seed = (int)rng.Randi();
        }
        GameManager.Instance.worldSeed = seed + "";
        terrainManager = new FlatTerrainManager(playerNode, lodThresholds, maxChunkThreads, seed);
        terrainManager.Name = "Terrain Manager";
        AddChild(terrainManager);
    }

}
