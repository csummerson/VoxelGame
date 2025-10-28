using Godot;
using System;

public partial class PlanetFaceOld : Node3D
{
	[Export] int chunkRadius;
	[Export] Vector3 localUp;
	[Export] PlanetConfig config;

	FaceChunk[,] chunks;

	int faceNum;

	public bool adaptive = false;

	public PlanetFaceOld(int chunkRadius, Vector3 localUp, PlanetConfig config, int faceNum)
	{
		this.chunkRadius = chunkRadius;
		this.localUp = localUp;
		this.config = config;
		this.faceNum = faceNum;
		chunks = new FaceChunk[chunkRadius * 2, chunkRadius * 2];
	}

	public override void _Ready()
	{
		Position = localUp * chunkRadius * WorldGenUtilities.chunkSize;

		Vector3 localRight = new Vector3(localUp.Y, localUp.Z, localUp.X);
		Vector3 localForward = -localUp.Cross(localRight); // godot sucks!
		Basis basis = new Basis(localRight, localUp, localForward);

		Transform3D transform = Transform3D.Identity;
		transform.Basis = basis;
		transform.Origin = Position;
		Transform = transform;

    }


    // FOR DEBUGGING ONLY AT SMALL VALUES!
	public void GenerateAllChunks()
	{
		for (int x = -chunkRadius; x < chunkRadius; x++)
		{
			for (int z = -chunkRadius; z < chunkRadius; z++)
			{
				FaceChunk chunk = new FaceChunk();
				chunk.blockRadius = 16 * chunkRadius;
				chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(x, 0, z));
				chunk.localBasis = Transform;
				chunk.adaptive = adaptive;
				AddChild(chunk);
				chunk.Generate();
				//MeshInstance3D mesh = chunk.GetChild<MeshInstance3D>(0);
				//StandardMaterial3D mat = new StandardMaterial3D();
				//mat.AlbedoColor = WorldGenUtilities.debugColors[faceNum];
				//mesh.MaterialOverride = mat;
				chunks[x + chunkRadius, z + chunkRadius] = chunk;
			}
		}
	}
}
