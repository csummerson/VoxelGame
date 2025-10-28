using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class ThreadedSurfaceNetter : Node3D
{
	[Export] int xSize = 16, ySize = 256, zSize = 16;
	[Export] bool adaptive = true, weighted = false;

	public Vector3I GlobalOffset = new Vector3I(0, 0, 0);

	private Action<ArrayMesh> _callback;

	public void GenerateAsync(Action<ArrayMesh> callback)
    {
        _callback = callback;

        Task.Run(() =>
        {
            ArrayMeshData meshData = BuildMesh();

            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = meshData.indices.ToArray();

            ArrayMesh arrMesh = new ArrayMesh();
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            CallDeferred(nameof(FinishGenerate), arrMesh);
        });
    }

	private void FinishGenerate(ArrayMesh arrMesh)
	{
        _callback?.Invoke(arrMesh);
		_callback = null;
	}

    // Making global due to their importance in helper methods
    //List<Vector3> vertsList = []; // vector positions in 3D space
    //List<Vector3> verts = [];
    //List<Vector3> normals = []; // Normal vectors for lighting calculations
    //List<int> indices = []; // Triangle list

    VolumetricField field;

    // Generates mesh from vertices
    private ArrayMeshData BuildMesh()
    {
        ArrayMeshData meshData = new ArrayMeshData();
        field = new VolumetricField();

        // Generating and placing vertexes
        for (int x = 0; x <= xSize; x++)
        {
            for (int y = 0; y <= ySize; y++)
            {
                for (int z = 0; z <= zSize; z++)
                {
                    Vector3 vertex = FindVertex(new Vector3I(x, y, z) + GlobalOffset);
                    meshData.vertsList.Add(vertex);

                    // lookup table
                    meshData.vertIndices[(x, y, z)] = meshData.vertsList.Count - 1;
                }
            }
        }

        // Generating mesh from vertexes
        for (int x = 1; x <= xSize; x++)
        {
            for (int y = 1; y <= ySize; y++)
            {
                for (int z = 1; z <= zSize; z++)
                {
                    // YZ faces
                    bool solidX1 = field.SampleVal(new Vector3(x + 0, y, z) + GlobalOffset, Vector3.Up) > 0;
                    bool solidX2 = field.SampleVal(new Vector3(x + 1, y, z) + GlobalOffset, Vector3.Up) > 0;
                    if (solidX1 != solidX2)
                    {
                        int v0 = meshData.vertIndices[(x, y - 1, z - 1)];
                        int v1 = meshData.vertIndices[(x, y - 0, z - 1)];
                        int v2 = meshData.vertIndices[(x, y - 0, z - 0)];
                        int v3 = meshData.vertIndices[(x, y - 1, z - 0)];
                        AddQuad(v0, v1, v2, v3, solidX2, meshData);
                    }

                    // XZ faces
                    bool solidY1 = field.SampleVal(new Vector3(x, y + 0, z) + GlobalOffset, Vector3.Up) > 0;
                    bool solidY2 = field.SampleVal(new Vector3(x, y + 1, z) + GlobalOffset, Vector3.Up) > 0;
                    if (solidY1 != solidY2)
                    {
                        int v0 = meshData.vertIndices[(x - 1, y, z - 1)];
                        int v1 = meshData.vertIndices[(x - 0, y, z - 1)];
                        int v2 = meshData.vertIndices[(x - 0, y, z - 0)];
                        int v3 = meshData.vertIndices[(x - 1, y, z - 0)];
                        AddQuad(v0, v1, v2, v3, solidY1, meshData);
                    }

                    // XY faces
                    bool solidZ1 = field.SampleVal(new Vector3(x, y, z) + GlobalOffset, Vector3.Up) > 0;
                    bool solidZ2 = field.SampleVal(new Vector3(x, y, z + 1) + GlobalOffset, Vector3.Up) > 0;
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

    // Helper method to generate quads
    private void AddQuad(int v0, int v1, int v2, int v3, bool flip, ArrayMeshData meshData)
    {
        Vector3 a = meshData.vertsList[v0];
        Vector3 b = meshData.vertsList[v1];
        Vector3 c = meshData.vertsList[v2];
        Vector3 d = meshData.vertsList[v3];

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
        // specific to function not mesh
        List<Vector3> normalsList = new List<Vector3>();

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

            float valA = field.SampleVal(a, Vector3.Up);
            float valB = field.SampleVal(b, Vector3.Up);

            if ((valA > 0) != (valB > 0))
            {
                Vector3 pos = a + (b - a) * (valA / (valA - valB)); // linear interp
                positions.Add(pos);

                if (weighted)
                {
                    Vector3 n = field.SampleGrad(pos);
                    normalsList.Add(n);
                }
            }
        }

        if (positions.Count == 0)
        {
            return new Vector3(p.X + 0.5f, p.Y + 0.5f, p.Z + 0.5f);
        }

        if (weighted)
        {
            return CentroidWeighted(positions, normalsList);
        }
        else
        {
            return Centroid(positions);
        }
    }

    Vector3 Centroid(List<Vector3> positions)
    {
        Vector3 sum = Vector3.Zero;

        foreach (Vector3 p in positions) {
            sum += p;
        }

        return sum / positions.Count;
    }

    Vector3 CentroidWeighted(List<Vector3> positions, List<Vector3> normals)
    {
        Vector3 sum = Vector3.Zero;
        float totalWeight = 0f;

        for (int i = 0; i < positions.Count; i++)
        {
            float weight = 1f + normals[i].LengthSquared();
            sum += positions[i] * weight;
            totalWeight += weight;
        }

        return sum / totalWeight;
    }
}

