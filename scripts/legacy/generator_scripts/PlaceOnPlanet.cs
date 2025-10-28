using Godot;
using System;

public partial class PlaceOnPlanet : Node3D
{
    [Export] int chunkRadius = 4;

    [Export] bool setParent = false;

    public override void _Ready()
    {
        if (setParent)
        {
            GetParentNode3D().Position = new Vector3(0, 2080, 0);
            return;
        }

        Position = new Vector3(0, (chunkRadius + 2) * 16, 0);
    }

}
