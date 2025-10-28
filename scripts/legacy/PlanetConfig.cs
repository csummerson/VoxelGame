using Godot;
using System;

public partial class PlanetConfig : Node3D
{
	[Export] int ChunkRadius = 16;

	Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Right, Vector3.Left, Vector3.Forward, Vector3.Back };
	Color[] colors = { Colors.White, Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple };

	[Export] bool adaptive = false;

	public override void _Ready()
	{
		for (int i = 0; i < 6; i++)
		{
			PlanetFaceOld face = new PlanetFaceOld(ChunkRadius, directions[i], this, i);
			face.Name = directions[i].ToString();
			face.adaptive = adaptive;
			AddChild(face);

			face.GenerateAllChunks();

			//break;
		}
	}
}
