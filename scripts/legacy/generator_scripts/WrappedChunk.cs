using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WrappedChunk : Node3D
{
	public bool smooth = GameManager.Instance.MODEL == (int)GameManager.WorldModel.Smooth;
	public int resolution = 2;
	public bool generateCollider = true;

	public Transform3D localBasis;

	public Transform3D transform;

	public int blockRadius = 4;

	VolumetricField field;

	public ArrayMeshData meshData;
	
	// Threaded functions
	private Action<ArrayMeshData> _callback;
	public void GenerateAsync(Action<ArrayMeshData> callback)
	{
		_callback = callback;

		field = GetNode<VolumetricField>("/root/Level/Planet/Planet Settings/Field");

		Vector3 position = Position;
		transform = Transform;

		Task.Run(() =>
        {
            meshData = GenerateData(position);
            CallDeferred(nameof(FinishGenerate));
        });
	}

	// Callback
	private void FinishGenerate()
	{
		_callback?.Invoke(meshData);
		_callback = null;
	}


	Vector3 localPosition;
	float[,,] samples;

	// Generates a data container to turn to an arraymesh
	public ArrayMeshData GenerateData(Vector3 position)
	{
		localPosition = position;

		int chunkWidth = WorldGenUtilities.chunkSize;
		int chunkHeight = WorldGenUtilities.chunkHeight;
		ArrayMeshData meshData = new ArrayMeshData(chunkWidth + 1, chunkHeight + 1, chunkWidth + 1);

		int W = chunkWidth + resolution;
		int H = chunkHeight + resolution;

		W = (W / resolution) + 1;
		H = (H / resolution) + 1;

		samples = new float[W + 1, H + 1, W + 1];
		

		for (int x = 0; x <= W; x++)
		{
			for (int y = 0; y <= H; y++)
			{
				for (int z = 0; z <= W; z++)
				{
					Vector3 snappedOrigin = localPosition.Snapped(Vector3.One * resolution);
					samples[x, y, z] = field.SampleValBasis((new Vector3(x, y, z) * resolution) + snappedOrigin, localBasis, blockRadius);
				}
			}
		}

		for (int x = 0; x < W; x++)
		{
			for (int y = 0; y < H; y++)
			{
				for (int z = 0; z < W; z++)
				{
					Vector3 vertex = FindVertex(new Vector3I(x, y, z));
					vertex = WorldGenUtilities.MapToSphere(vertex, blockRadius, transform); // just do it now
					meshData.vertsList.Add(vertex);

					// lookup table
					meshData.vertIndicesAr[x, y, z] = meshData.vertsList.Count - 1;
				}
			}
		}

		for (int x = 1; x < W; x++)
		{
			for (int y = 1; y < H; y++)
			{
				for (int z = 1; z < W; z++)
				{
					// YZ faces	
					bool solidX1 = samples[x + 0, y, z] > 0;
					bool solidX2 = samples[x + 1, y, z] > 0;
					if (solidX1 != solidX2)
					{
						int v0 = meshData.vertIndicesAr[x, y - 1, z - 1];
						int v1 = meshData.vertIndicesAr[x, y - 0, z - 1];
						int v2 = meshData.vertIndicesAr[x, y - 0, z - 0];
						int v3 = meshData.vertIndicesAr[x, y - 1, z - 0];
						AddQuad(v0, v1, v2, v3, solidX2, meshData);
					}

					// XZ faces
					bool solidY1 = samples[x, y + 0, z] > 0;
					bool solidY2 = samples[x, y + 1, z] > 0;
					if (solidY1 != solidY2)
					{
						int v0 = meshData.vertIndicesAr[x - 1, y, z - 1];
						int v1 = meshData.vertIndicesAr[x - 0, y, z - 1];
						int v2 = meshData.vertIndicesAr[x - 0, y, z - 0];
						int v3 = meshData.vertIndicesAr[x - 1, y, z - 0];
						AddQuad(v0, v1, v2, v3, solidY1, meshData);
					}

					// XY faces
					bool solidZ1 = samples[x, y, z + 0] > 0;
					bool solidZ2 = samples[x, y, z + 1] > 0;
					if (solidZ1 != solidZ2)
					{
						int v0 = meshData.vertIndicesAr[x - 1, y - 1, z];
						int v1 = meshData.vertIndicesAr[x - 0, y - 1, z];
						int v2 = meshData.vertIndicesAr[x - 0, y - 0, z];
						int v3 = meshData.vertIndicesAr[x - 1, y - 0, z];
						AddQuad(v0, v1, v2, v3, solidZ2, meshData);
					}
				}
			}
		}

		return meshData;
	}
	
	private void AddQuad(int a, int b, int c, int d, bool flip, ArrayMeshData meshData)
    {
		if (flip)
		{
			AddTriangle(a, b, c, meshData);
			AddTriangle(c, d, a, meshData);
		}
		else
		{
			AddTriangle(c, b, a, meshData);
			AddTriangle(a, d, c, meshData);
		}
    }

    private void AddTriangle(int a, int b, int c, ArrayMeshData meshData)
    {
        int startIndex = meshData.verts.Count;

		Vector3 v0 = meshData.vertsList[a];
		Vector3 v1 = meshData.vertsList[b];
		Vector3 v2 = meshData.vertsList[c];

        meshData.verts.Add(v0);
        meshData.verts.Add(v1);
        meshData.verts.Add(v2);

		if (generateCollider)
		{
			meshData.collisionVerts.Add(v0);
			meshData.collisionVerts.Add(v1);
			meshData.collisionVerts.Add(v2);
		}

        Vector3 normal = (v2 - v0).Cross(v1 - v0).Normalized();
        meshData.normals.Add(normal);
        meshData.normals.Add(normal);
        meshData.normals.Add(normal);

        meshData.indices.Add(startIndex);
        meshData.indices.Add(startIndex + 1);
        meshData.indices.Add(startIndex + 2);
    }

    // Placing the verts
    private Vector3 FindVertex(Vector3I p)
    {
        if (!smooth)
        {
            return new Vector3(p.X + 0.5f, p.Y + 0.5f, p.Z + 0.5f) * resolution;
        }
		
		Vector3 sum = Vector3.Zero;
    	int count = 0;

        for (int i = 0; i < 12; i++)
		{
			var edge1 = WorldGenUtilities.edgePairs[i, 0];
			var edge2 = WorldGenUtilities.edgePairs[i, 1];

			var cA = WorldGenUtilities.cornerOffsets[edge1];
			var cB = WorldGenUtilities.cornerOffsets[edge2];

			int ax = p.X + cA.x, ay = p.Y + cA.y, az = p.Z + cA.z;
			int bx = p.X + cB.x, by = p.Y + cB.y, bz = p.Z + cB.z;

			float valA = samples[ax, ay, az];
			float valB = samples[bx, by, bz];

			if ((valA > 0) != (valB > 0))
			{
				float t = valA / (valA - valB); 

				float ix = cA.x + (cB.x - cA.x) * t + p.X;
				float iy = cA.y + (cB.y - cA.y) * t + p.Y;
				float iz = cA.z + (cB.z - cA.z) * t + p.Z;

				sum += new Vector3(ix, iy, iz);
				count++;
			}
		}

        if (count == 0)
        {
            return new Vector3(p.X + 0.5f, p.Y + 0.5f, p.Z + 0.5f) * resolution;
        }
		return (sum / count) * resolution;
    }

    Vector3 Centroid(List<Vector3> positions)
    {
        Vector3 sum = Vector3.Zero;

        foreach (Vector3 p in positions) {
            sum += p;
        }

        return sum / positions.Count;
    }
}