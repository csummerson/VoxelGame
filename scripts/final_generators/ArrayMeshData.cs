using Godot;
using System.Collections.Generic;

public partial class ArrayMeshData : GodotObject
{
    public int[,,] vertIndicesAr;

    public Vector3[] vertsList;
    public List<Vector3> verts;
    public List<Vector3> vertexNormals;
    public List<int> indices;
    public List<Vector3> collisionVerts;

    // material list
    public Dictionary<byte, List<int>> materialIndices;

    public ArrayMeshData(int x, int y, int z)
    {
        vertIndicesAr = new int[x + 1, y + 1, z + 1];
        vertsList = new Vector3[x * y * z];
        verts = new();
        vertexNormals = new();
        indices = new List<int>();
        collisionVerts = new();
        materialIndices = new();
        pointCountY = y;
        pointCountZ = z;
    }

    public int pointCountY;
    public int pointCountZ;

    public int LinearIndex(int x, int y, int z)
    {
        return x * (pointCountY * pointCountZ) + y * pointCountZ + z;
    }

    public void Clear()
    {
        verts?.Clear();
        vertexNormals?.Clear();
        indices?.Clear();
        collisionVerts?.Clear();

        foreach (var kvp in materialIndices)
            kvp.Value.Clear();

        materialIndices?.Clear();

        vertsList = null;
        vertIndicesAr = null;
    }
}