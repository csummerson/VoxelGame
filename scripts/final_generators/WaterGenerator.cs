using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class WaterGenerator
{
    int chunkSize = WorldGenUtility.chunkSize + 1;
    List<Vector3> verts;
    List<Vector3> normals;
    List<int> indices;

    int waterHeight = 192;

    int resolution = 1;


    public WaterGenerator(int resolution)
    {
        verts = [];
        normals = [];
        indices = [];
        this.resolution = resolution;
    }

    public ArrayMesh GenerateMesh()
    {
        Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                verts.Add(new Vector3(x * resolution, waterHeight, z * resolution));
                normals.Add(Vector3.Up);
            }
        }

        for (int i = 0; i < chunkSize - 1; i++)
        {
            for (int j = 0; j < chunkSize - 1; j++)
            {
                int rowVerts = chunkSize;

                int bottomLeft = j + i * rowVerts;
                int bottomRight = j + 1 + i * rowVerts;
                int topLeft = j + (i + 1) * rowVerts;
                int topRight = j + 1 + (i + 1) * rowVerts;

                indices.Add(bottomLeft);
                indices.Add(topLeft);
                indices.Add(bottomRight);

                indices.Add(topLeft);
                indices.Add(topRight);
                indices.Add(bottomRight);
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        MeshInstance3D meshInstance = new MeshInstance3D();
        ArrayMesh arrMesh = new ArrayMesh();
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        return arrMesh;
    }
}
