using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class SquareGenerator : Node3D
{
	// A script designed to understand mesh instantiation in Godot.
	public override void _Ready()
    {
        // This array contains all data
        Array surfaceArray = [];
        // Size godot expects it to be
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> verts = []; // vector positions in 3D space
        List<Vector2> uvs = []; // UV coordinates for texturing
        List<Vector3> normals = []; // Normal vectors for lighting calculations
        List<int> indices = []; // Triangle list

        // -- GENERATE MESH! --
        verts.Add(new Vector3(0, 0, 0));
        verts.Add(new Vector3(0, 0, 1));
        verts.Add(new Vector3(1, 0, 0));
        verts.Add(new Vector3(1, 0, 1));

        // Triangle 1
        indices.Add(0);
        indices.Add(1);
        indices.Add(2);

        // Triangle 2
        indices.Add(1);
        indices.Add(3);
        indices.Add(2);

        // One normal per vertex
        normals.Add(Vector3.Up);
        normals.Add(Vector3.Up);
        normals.Add(Vector3.Up);
        normals.Add(Vector3.Up);

        // One uv per vertex
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));

        // Conversions
        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        MeshInstance3D meshInstance = new MeshInstance3D();
        ArrayMesh arrMesh = meshInstance.Mesh as ArrayMesh;
        if (arrMesh != null)
        {
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        }
        AddChild(meshInstance);
    }
}
