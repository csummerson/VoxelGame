using Godot;
using System;

public partial class RaySphere : Camera3D
{
    [Export] Node3D sphere;
    [Export] int length = 5;
    [Export] public int size = 2;
    public Vector3 brushPoint;

    public override void _Ready()
    {
        sphere.Scale = Vector3.One * size * size;
    }

    public override void _PhysicsProcess(double delta)
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        Vector3 from = GlobalPosition;
        Vector3 to = from + -GlobalTransform.Basis.Z * length;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        Vector3 spawnPoint;

        if (result.Count > 0)
        {
            spawnPoint = (Vector3)result["position"];
            brushPoint = spawnPoint;
            //sphere.Visible = true;

            if (Input.IsActionJustPressed("mouse_left"))
            {
                var collider = (Node3D)result["collider"];
                if (collider == null)
                    return;

                var parent = collider.GetParentNode3D();
                if (parent is ChunkManager chunkManager)
                {
                    chunkManager.DeformGlobal(brushPoint, size, -10);
                    //GD.Print("Attempted to deform something.");
                }
            }
            else if (Input.IsActionJustPressed("mouse_right"))
            {
                var collider = (Node3D)result["collider"];
                if (collider == null)
                    return;

                var parent = collider.GetParentNode3D();
                if (parent is ChunkManager chunkManager)
                {
                    chunkManager.DeformGlobal(brushPoint, size, 10);
                    //GD.Print("Attempted to form something.");
                }
            }
        }
        else
        {
            spawnPoint = Vector3.Zero;
            brushPoint = Vector3.Zero;
            sphere.Visible = false;
        }

        sphere.GlobalPosition = spawnPoint;
    }

}
