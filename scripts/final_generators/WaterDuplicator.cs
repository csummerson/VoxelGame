using Godot;
using System;

public partial class WaterDuplicator : Node3D
{
    [Export] int size;
    [Export] int waterSize = 16;
    [Export] int layer = 58;
    [Export] PackedScene waterChunk;

    public override void _Ready()
    {
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                Vector3 position = new Vector3(x * waterSize, layer, y * waterSize);
                Node3D chunk = (Node3D) waterChunk.Instantiate();
                chunk.Position = position;
                AddChild(chunk);
            }
        }
    }
}
