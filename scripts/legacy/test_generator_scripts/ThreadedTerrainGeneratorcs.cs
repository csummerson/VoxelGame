using Godot;
using System;
using System.Collections.Generic;

public partial class ThreadedTerrainGeneratorcs : Node3D
{
	[Export] public Camera3D TargetCamera;
    [Export] public PackedScene ThreadedMCScene;
    [Export] public int RenderDistance = 4;

    private Dictionary<Vector3I, ThreadedMC> activeChunks = new();

    public override void _Process(double delta)
    {
        if (TargetCamera == null) return;

        Vector3I camChunk = WorldToChunk(TargetCamera.GlobalPosition);
        HashSet<Vector3I> needed = new();

        for (int x = -RenderDistance; x <= RenderDistance; x++)
        {
            for (int z = -RenderDistance; z <= RenderDistance; z++)
            {
				if (MathF.Pow(x, 2) + (MathF.Pow(z, 2)) < MathF.Pow(RenderDistance,2))
				{
					Vector3I c = camChunk + new Vector3I(x, 0, z);
					needed.Add(c);

					if (!activeChunks.ContainsKey(c))
					{
						SpawnChunk(c);
					}
				}
            }
        }

        
        var toRemove = new List<Vector3I>();
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

    private void SpawnChunk(Vector3I coord)
    {
        var chunk = ThreadedMCScene.Instantiate<ThreadedMC>();
        chunk.offset = new Vector3I(
            coord.X * 16,
            coord.Y * 0,
            coord.Z * 16
        );
        chunk.Position = chunk.offset; 
		AddChild(chunk);

		chunk.GenerateAsync((arrMesh) =>
		{
			ArrayMesh arrayMesh = arrMesh;

			MeshInstance3D meshInstance = new MeshInstance3D();
			meshInstance.Mesh = arrMesh;

			StandardMaterial3D mat = new StandardMaterial3D();
			mat.VertexColorUseAsAlbedo = true;
			mat.VertexColorIsSrgb = false;
			meshInstance.MaterialOverride = mat;

			chunk.AddChild(meshInstance);
		});

        activeChunks[coord] = chunk;
    }

    private Vector3I WorldToChunk(Vector3 pos)
    {
        return new Vector3I(
            Mathf.FloorToInt(pos.X / 16),
			0,
            Mathf.FloorToInt(pos.Z / 16)
        );
    }
}
