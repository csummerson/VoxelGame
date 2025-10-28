using Godot;
using System;
using System.Collections.Generic;

public partial class PlanetTerrainGenerator : Node3D
{
	[Export] PlanetSettings settings;

	int chunkRadius;

	PlanetFace[] faces = new PlanetFace[6];

	Vector3[] directions = { Vector3.Right, Vector3.Left, Vector3.Up, Vector3.Down, Vector3.Forward, Vector3.Back };

	Dictionary<(PlanetFace, int, int), FaceChunk> activeChunks = new();

	[Export] int renderDistance = 16;

	public override void _Ready()
	{
		chunkRadius = settings.chunkRadius;

		for (int i = 0; i < faces.Length; i++)
		{
			PlanetFace face = new PlanetFace(directions[i], chunkRadius, (CubeFace)i);
			face.Name = directions[i].ToString();
			AddChild(face);
			faces[i] = face;
			//face.LoadAllChunks(); // DEBUG ONLY!
		}
	}

	public override void _Process(double delta)
	{
		Camera3D cam = GetViewport().GetCamera3D();
		Vector3 playerPosition = cam.GlobalPosition;
		CubeFace cubeFace = GetFace(playerPosition);

		PlanetFace dominantFace = faces[(int)cubeFace];

		Vector2I chunkCoordinate = WorldGenUtilities.SphereToChunk(playerPosition, cubeFace, chunkRadius);
		//var key = (dominantFace, chunkCoordinate.X, chunkCoordinate.Y);

		HashSet<(PlanetFace, int, int)> needed = new();
		foreach (var coord in SpiralCoords(chunkCoordinate, renderDistance))
		{
			var key = (dominantFace, coord.X, coord.Y);
			needed.Add(key);

			if (!activeChunks.ContainsKey(key))
			{
				// Generation
				//FaceChunk chunk = dominantFace.LoadChunk(coord); // DEBUG ONLY
				FaceChunk chunk = dominantFace.LoadChunkAsync(coord, true); 
				activeChunks.Add(key, chunk);
			}
		}

		var toRemove = new List<(PlanetFace, int, int)>();
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

	IEnumerable<Vector2I> SpiralCoords(Vector2I center, int radius)
	{
		yield return center; // start with center

		for (int r = 1; r <= radius; r++)
		{
			// Top edge
			for (int x = center.X - r; x <= center.X + r; x++)
				yield return new Vector2I(x, center.Y - r);

			// Right edge
			for (int y = center.Y - r + 1; y <= center.Y + r; y++)
				yield return new Vector2I(center.X + r, y);

			// Bottom edge
			for (int x = center.X + r - 1; x >= center.X - r; x--)
				yield return new Vector2I(x, center.Y + r);

			// Left edge
			for (int y = center.Y + r - 1; y > center.Y - r; y--)
				yield return new Vector2I(center.X - r, y);
		}
	}

	private CubeFace GetFace(Vector3 position)
	{
		Vector3 absolute = position.Abs();
		Vector3.Axis maxAxis = absolute.MaxAxisIndex();
		int axisSign = Mathf.Sign(position[(int)maxAxis]);

		return (maxAxis, axisSign) switch
		{
			(Vector3.Axis.X, 1) => CubeFace.PosX,
			(Vector3.Axis.X, -1) => CubeFace.NegX,
			(Vector3.Axis.Y, 1) => CubeFace.PosY,
			(Vector3.Axis.Y, -1) => CubeFace.NegY,
			// Again, godot is weird with +/- Z. Back is technically forward in Godot, which is still dumb imo.
			(Vector3.Axis.Z, -1) => CubeFace.PosZ,
			(Vector3.Axis.Z, 1) => CubeFace.NegZ,
			_ => CubeFace.PosY
		};
	}
}

public enum CubeFace
{
	PosX, NegX,
	PosY, NegY,
	PosZ, NegZ
}
