using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class PlaneGenerator : Node3D
{
	[Export] int size = 16;

	/*
	Further research indicates the surface tool is somewhat inefficient, so we will instead move to ArrayMesh.
	*/

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

		// Vertice, normal, and uv generation
		for (int i = 0; i < size + 1; i++)
		{
			for (int j = 0; j < size + 1; j++)
			{
				float half = (size - 1) / 2f;
				verts.Add(new Vector3(j - half, 0, i - half));
				normals.Add(Vector3.Up);
				uvs.Add(new Vector2((float)j / size, (float)i / size));
			}
		}

		// Triangle generation
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				int rowVerts = size + 1;

				int bottomLeft = j + i * rowVerts;
				int bottomRight = j + 1 + i * rowVerts;
				int topLeft = j + (i + 1) * rowVerts;
				int topRight = j + 1 + (i + 1) * rowVerts;


				// Bottom left
				indices.Add(bottomLeft); // start
				indices.Add(bottomRight); // right 1
				indices.Add(topLeft); // up a row

				// Top right
				indices.Add(topLeft);
				indices.Add(bottomRight);
				indices.Add(topRight);
			}
		}

		// Conversions
		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		MeshInstance3D meshInstance = new MeshInstance3D();
		ArrayMesh arrMesh = new ArrayMesh();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		meshInstance.Mesh = arrMesh;
		AddChild(meshInstance);
	}
}
