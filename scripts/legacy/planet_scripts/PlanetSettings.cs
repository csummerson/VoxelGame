using Godot;
using System;

public partial class PlanetSettings : Node3D
{
    [Export] public int chunkRadius = 16;
    [Export] public int simulationDistance = 8;
    [Export] public int renderDistance = 4;
    [Export] public VolumetricField field;
    [Export] public int seed;

    public override void _EnterTree()
    {
        // if (!OS.HasFeature("debug"))
        // {
            chunkRadius = GameManager.Instance.SIZE;
            renderDistance = GameManager.Instance.RD;
        simulationDistance = GameManager.Instance.SD;
        // }

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        seed = (int)rng.Randi();

        field = GetNode<VolumetricField>("Field");
        field.seed = seed;
    }
}
