using Godot;
using System;
using System.Collections.Generic;

public partial class CubeSphereGenerator : Node3D
{
	[Export] int resolution = 16;

	[Export] bool morph = true;

	Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

	public override void _Ready()
	{
		//return;
		SphereFace[] faces = new SphereFace[6];

		for (int i = 0; i < 6; i++)
		{
			faces[i] = new SphereFace(resolution, directions[i], morph);
			MeshInstance3D meshInstance = new MeshInstance3D();
			meshInstance.Mesh = faces[i].GenerateMesh();
			meshInstance.Position = meshInstance.Position + directions[i] / 64; // slight gab for debugging
			AddChild(meshInstance);
		}
	}
}
