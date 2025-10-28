using Godot;
using System.Collections.Generic;

public partial class ArrayMeshData : GodotObject
{
    public Dictionary<(int, int, int), int> vertIndices;

    public int[,,] vertIndicesAr;

    public List<Vector3> vertsList;
    public List<Vector3> verts;
    public List<Vector3> normals;
    public List<int> indices;
    public List<Vector3> collisionVerts;

    // material list
    public Dictionary<byte, List<int>> materialIndices;

    public ArrayMeshData()
    {
        vertIndices = new();
        vertsList = new();
        verts = new();
        normals = new();
        indices = new List<int>();
        collisionVerts = new();
    }

    public ArrayMeshData(int x, int y, int z)
    {
        vertIndicesAr = new int[x + 1, y + 1, z + 1];
        vertsList = new();
        verts = new();
        normals = new();
        indices = new List<int>();
        collisionVerts = new();
        materialIndices = new();
    }
}