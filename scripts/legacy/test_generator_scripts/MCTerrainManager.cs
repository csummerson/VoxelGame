using Godot;
using System;
using System.Collections.Generic;

public partial class MCTerrainManager : Node3D
{
	[Export] public int renderDistance = 16;
	[Export] public PackedScene MarchingCubesScene;
	[Export] Camera3D camera;
	
	private Dictionary<Vector3I, MarchingCubes> activeChunks = new();

	public override void _Process(double delta)
	{
		Vector3I cameraChunkPos = WorldToChunk(camera.GlobalPosition);
		HashSet<Vector3I> newChunks = new();

		for (int x = -renderDistance; x <= renderDistance; x++)
		{
			for (int z = -renderDistance; z <= renderDistance; z++)
			{
				Vector3I chunk = cameraChunkPos + new Vector3I(x, 0, z);
				newChunks.Add(chunk);

				if (!activeChunks.ContainsKey(chunk))
				{
					SpawnChunk(chunk);
				}
			}
		}

		foreach (var chunk in activeChunks)
		{
			if (!newChunks.Contains(chunk.Key))
			{
				chunk.Value.QueueFree();
				activeChunks.Remove(chunk.Key);
			}
		}

	}

	private void SpawnChunk(Vector3I chunkPos)
	{
		MarchingCubes chunk = MarchingCubesScene.Instantiate<MarchingCubes>();
		chunk.offset = new Vector3I(
			chunkPos.X * 16,
			0,
			chunkPos.Z * 16
		);
		chunk.Position = chunk.offset;
		AddChild(chunk);
		activeChunks[chunkPos] = chunk;
	}

	private Vector3I WorldToChunk(Vector3 p)
	{
		int x = Mathf.FloorToInt(p.X / 16);
		int y = 0;
		int z = Mathf.FloorToInt(p.Z / 16);

		return new Vector3I(x, y, z);
	}

}
