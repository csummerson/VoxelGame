using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class DualContouringTest : Node3D
{
    [Export] int sizeX = 16, sizeY = 64, sizeZ = 16;

    [Export] bool adaptive = false, weighted = false;

    VolumetricField field;

    // Making global due to their importance in helper methods
    List<Vector3> vertsList = []; // vector positions in 3D space
    List<Vector3> verts = [];
    List<Vector3> normals = []; // Normal vectors for lighting calculations
    List<int> indices = []; // Triangle list

    public override void _Ready()
    {
        field = new VolumetricField();

        DualThemContours();
    }

    // Generates mesh from vertices
    private void DualThemContours()
    {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        Dictionary<(int, int, int), int> vertIndices = new();

        // Generating and placing vertexes
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3 vertex = FindVertex(x, y, z);
                    vertsList.Add(vertex);

                    // lookup table
                    vertIndices[(x, y, z)] = vertsList.Count - 1;
                }
            }
        }

        // Generating mesh from vertexes
        for (int x = 1; x < sizeX; x++)
        {
            for (int y = 1; y < sizeY; y++)
            {
                for (int z = 1; z < sizeZ; z++)
                {
                    // YZ faces
                    bool solidX1 = field.SampleVal(new Vector3(x + 0, y, z), Vector3.Up) > 0;
                    bool solidX2 = field.SampleVal(new Vector3(x + 1, y, z), Vector3.Up) > 0;
                    if (solidX1 != solidX2)
                    {
                        int v0 = vertIndices[(x, y - 1, z - 1)];
                        int v1 = vertIndices[(x, y - 0, z - 1)];
                        int v2 = vertIndices[(x, y - 0, z - 0)];
                        int v3 = vertIndices[(x, y - 1, z - 0)];
                        AddQuad(v0, v1, v2, v3, solidX2);
                    }

                    // XZ faces
                    bool solidY1 = field.SampleVal(new Vector3(x, y + 0, z), Vector3.Up) > 0;
                    bool solidY2 = field.SampleVal(new Vector3(x, y + 1, z), Vector3.Up) > 0;
                    if (solidY1 != solidY2)
                    {
                        int v0 = vertIndices[(x - 1, y, z - 1)];
                        int v1 = vertIndices[(x - 0, y, z - 1)];
                        int v2 = vertIndices[(x - 0, y, z - 0)];
                        int v3 = vertIndices[(x - 1, y, z - 0)];
                        AddQuad(v0, v1, v2, v3, solidY1);
                    }

                    // XY faces
                    bool solidZ1 = field.SampleVal(new Vector3(x, y, z), Vector3.Up) > 0;
                    bool solidZ2 = field.SampleVal(new Vector3(x, y, z + 1), Vector3.Up) > 0;
                    if (solidZ1 != solidZ2)
                    {
                        int v0 = vertIndices[(x - 1, y - 1, z)];
                        int v1 = vertIndices[(x - 0, y - 1, z)];
                        int v2 = vertIndices[(x - 0, y - 0, z)];
                        int v3 = vertIndices[(x - 1, y - 0, z)];
                        AddQuad(v0, v1, v2, v3, solidZ2);
                    }
                }
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        MeshInstance3D meshInstance = new MeshInstance3D();
        ArrayMesh arrMesh = new ArrayMesh();
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        meshInstance.Mesh = arrMesh;

        AddChild(meshInstance);
    }

    // Helper method to generate quads
    private void AddQuad(int v0, int v1, int v2, int v3, bool flip)
    {
        Vector3 a = vertsList[v0];
        Vector3 b = vertsList[v1];
        Vector3 c = vertsList[v2];
        Vector3 d = vertsList[v3];

        if (flip)
        {
            AddTriangle(a, b, c);
            AddTriangle(c, d, a);
        }
        else
        {
            AddTriangle(c, b, a);
            AddTriangle(a, d, c);
        }
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int startIndex = verts.Count;

        verts.Add(a);
        verts.Add(b);
        verts.Add(c);

        Vector3 normal = (c - a).Cross(b - a).Normalized();
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        indices.Add(startIndex);
        indices.Add(startIndex + 1);
        indices.Add(startIndex + 2);
    }

    // Placing the verts
    private Vector3 FindVertex(int x, int y, int z)
    {
        if (!adaptive)
        {
            return new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
        }

        List<Vector3> positions = new List<Vector3>();
        // specific to function not mesh
        List<Vector3> normalsList = new List<Vector3>();

        Vector3[] cubeCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            int cx = x + ((i & 1) == 1 ? 1 : 0);
            int cy = y + ((i & 2) == 2 ? 1 : 0);
            int cz = z + ((i & 4) == 4 ? 1 : 0);
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
                Vector3 p = a + (b - a) * (valA / (valA - valB)); // linear interp
                Vector3 n = field.SampleGrad(p);
                positions.Add(p);
                normalsList.Add(n);
            }
        }

        if (positions.Count == 0)
        {
            return new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
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



