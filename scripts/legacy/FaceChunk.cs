using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class FaceChunk : Node3D
{
	public bool adaptive = false;
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

		field = new VolumetricField();
		AddChild(field);

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

	// Single threaded
	public void Generate()
	{
		field = new VolumetricField();
		AddChild(field);

		ArrayMeshData meshData = GenerateData(Position);

		Godot.Collections.Array surfaceArray = [];
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = meshData.indices.ToArray();

		ArrayMesh arrMesh = new ArrayMesh();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		MeshInstance3D meshInstance = new MeshInstance3D();
		meshInstance.Mesh = arrMesh;
		meshInstance.Name = "Mesh";
		AddChild(meshInstance);

		if (generateCollider)
		{
			// Body object to interact with physics
			StaticBody3D body = new StaticBody3D();
			AddChild(body);

			// Collider mesh
			CollisionShape3D collider = new CollisionShape3D();
			ConcavePolygonShape3D colliderMesh = new ConcavePolygonShape3D();
			colliderMesh.SetFaces(meshData.collisionVerts.ToArray());
			collider.Shape = colliderMesh;
			body.AddChild(collider);
		}
	}

	Vector3I localPosition;

	public ArrayMeshData GenerateData(Vector3 position)
	{
		localPosition = (Vector3I)new Vector3(position.X, position.Y, position.Z);

		ArrayMeshData meshData = new ArrayMeshData();

		int chunkWidth = WorldGenUtilities.chunkSize;
		int chunkHeight = WorldGenUtilities.chunkHeight;

		for (int x = 0; x <= chunkWidth; x++)
		{
			for (int y = 0; y <= chunkHeight; y++)
			{
				for (int z = 0; z <= chunkWidth; z++)
				{
					Vector3 vertex = FindVertex(new Vector3I(x, y, z));
					meshData.vertsList.Add(vertex);

					// lookup table
					meshData.vertIndices[(x, y, z)] = meshData.vertsList.Count - 1;
				}
			}
		}

		// debug
		//return meshData;

		for (int x = 1; x <= chunkWidth; x++)
		{
			for (int y = 1; y <= chunkHeight; y++)
			{
				for (int z = 1; z <= chunkWidth; z++)
				{
					// YZ faces	
					bool solidX1 = field.SampleValBasis(new Vector3(x + 0, y, z) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					bool solidX2 = field.SampleValBasis(new Vector3(x + 1, y, z) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					if (solidX1 != solidX2)
					{
						int v0 = meshData.vertIndices[(x, y - 1, z - 1)];
						int v1 = meshData.vertIndices[(x, y - 0, z - 1)];
						int v2 = meshData.vertIndices[(x, y - 0, z - 0)];
						int v3 = meshData.vertIndices[(x, y - 1, z - 0)];
						AddQuad(v0, v1, v2, v3, solidX2, meshData);
					}

					// XZ faces
					bool solidY1 = field.SampleValBasis(new Vector3(x, y + 0, z) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					bool solidY2 = field.SampleValBasis(new Vector3(x, y + 1, z) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					if (solidY1 != solidY2)
					{
						int v0 = meshData.vertIndices[(x - 1, y, z - 1)];
						int v1 = meshData.vertIndices[(x - 0, y, z - 1)];
						int v2 = meshData.vertIndices[(x - 0, y, z - 0)];
						int v3 = meshData.vertIndices[(x - 1, y, z - 0)];
						AddQuad(v0, v1, v2, v3, solidY1, meshData);
					}

					// XY faces
					bool solidZ1 = field.SampleValBasis(new Vector3(x, y, z + 0) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					bool solidZ2 = field.SampleValBasis(new Vector3(x, y, z + 1) + localPosition - Vector3.One, localBasis, blockRadius) > 0;
					if (solidZ1 != solidZ2)
					{
						int v0 = meshData.vertIndices[(x - 1, y - 1, z)];
						int v1 = meshData.vertIndices[(x - 0, y - 1, z)];
						int v2 = meshData.vertIndices[(x - 0, y - 0, z)];
						int v3 = meshData.vertIndices[(x - 1, y - 0, z)];
						AddQuad(v0, v1, v2, v3, solidZ2, meshData);
					}
				}
			}
		}

		return meshData;
	}
	
	private void AddQuad(int v0, int v1, int v2, int v3, bool flip, ArrayMeshData meshData)
    {
		Vector3 a = meshData.vertsList[v0];
        Vector3 b = meshData.vertsList[v1];
        Vector3 c = meshData.vertsList[v2];
        Vector3 d = meshData.vertsList[v3];

		a = WorldGenUtilities.MapToSphere(a, blockRadius, transform);
		b = WorldGenUtilities.MapToSphere(b, blockRadius, transform);
		c = WorldGenUtilities.MapToSphere(c, blockRadius, transform);
		d = WorldGenUtilities.MapToSphere(d, blockRadius, transform);

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

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, ArrayMeshData meshData)
    {
        int startIndex = meshData.verts.Count;

        meshData.verts.Add(a);
        meshData.verts.Add(b);
        meshData.verts.Add(c);

		if (generateCollider)
		{
			meshData.collisionVerts.Add(a);
			meshData.collisionVerts.Add(b);
			meshData.collisionVerts.Add(c);
		}

        Vector3 normal = (c - a).Cross(b - a).Normalized();
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
        if (!adaptive)
        {
            return new Vector3(p.X + 0.5f, p.Y + 0.5f, p.Z + 0.5f);
        }

        List<Vector3> positions = new List<Vector3>();

        Vector3[] cubeCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            int cx = p.X + ((i & 1) == 1 ? 1 : 0);
            int cy = p.Y + ((i & 2) == 2 ? 1 : 0);
            int cz = p.Z + ((i & 4) == 4 ? 1 : 0);
            cubeCorners[i] = new Vector3(cx, cy, cz);
        }

        int[,] edges = {
            {0,1},{1,3},{3,2},{2,0}, // bottom
            {4,5},{5,7},{7,6},{6,4}, // top
            {0,4},{1,5},{2,6},{3,7}  // vertical
        };

        for (int i = 0; i < 12; i++)
        {
            Vector3 a = cubeCorners[edges[i, 0]];
            Vector3 b = cubeCorners[edges[i, 1]];

            float valA = field.SampleValBasis(a + localPosition - Vector3.One, localBasis, blockRadius);
            float valB = field.SampleValBasis(b + localPosition - Vector3.One, localBasis, blockRadius);

            if ((valA > 0) != (valB > 0))
            {
                Vector3 pos = a + (b - a) * (valA / (valA - valB)); // linear interp
                positions.Add(pos);
            }
        }

        if (positions.Count == 0)
        {
            return new Vector3(p.X + 0.5f, p.Y + 0.5f, p.Z + 0.5f);
        }
        return Centroid(positions);
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
