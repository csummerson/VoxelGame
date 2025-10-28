using Godot;
using System;
using System.Collections.Generic;

public class SphereFace
{
	int resolution;
	Vector3 localUp;

	Vector3 axisA, axisB;

	bool morph;

	public SphereFace(int resolution, Vector3 localUp, bool morph)
	{
		this.resolution = resolution;
		this.localUp = localUp;
		this.morph = morph;
		axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
		axisB = localUp.Cross(axisA);
	}

	public ArrayMesh GenerateMesh()
	{
		List<Vector3> vertsList = [];
		List<Vector3> verts = [];
		List<Vector3> normals = [];
		List<int> indices = [];

		for (int x = 0; x < resolution; x++)
		{
			for (int y = 0; y < resolution; y++)
			{
				Vector2 percent = new Vector2(x, y) / (resolution - 1);
				Vector3 pointOnUnitCube = localUp + (percent.X - 0.5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
				//Vector3 pointOnUnitSphere = pointOnUnitCube.Normalized();

				if (morph)
				{
					Vector3 pointOnUnitSphere = WorldGenUtilities.MapToSphere(pointOnUnitCube, 1, 1);
					vertsList.Add(pointOnUnitSphere);
				}
				else
				{
					vertsList.Add(pointOnUnitCube);
				}
			}
		}

		// For flat shading
		for (int x = 0; x < resolution - 1; x++)
		{
			for (int y = 0; y < resolution - 1; y++)
			{
				int bottomLeft = x * resolution + y;
				int bottomRight = bottomLeft + 1;
				int topLeft = bottomLeft + resolution;
				int topRight = bottomLeft + resolution + 1;

				Vector3 a = vertsList[bottomLeft];
				Vector3 b = vertsList[bottomRight];
				Vector3 c = vertsList[topLeft];
				Vector3 d = vertsList[topRight];

				int startIndex = verts.Count;

				// triangle 1
				verts.Add(a);
				verts.Add(b);
				verts.Add(c);
				indices.Add(startIndex);
				indices.Add(startIndex + 1);
				indices.Add(startIndex + 2);

				Vector3 normal = -(b - a).Cross(c - a).Normalized();

				normals.Add(normal);
				normals.Add(normal);
				normals.Add(normal);

				// triangle 2 flipped
				verts.Add(c);
				verts.Add(b);
				verts.Add(d);
				indices.Add(startIndex + 3);
				indices.Add(startIndex + 4);
				indices.Add(startIndex + 5);

				normal = -(b - c).Cross(d - c).Normalized();

				normals.Add(normal);
				normals.Add(normal);
				normals.Add(normal);
			}
		}

		Godot.Collections.Array surfaceArray = [];
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		ArrayMesh arrMesh = new ArrayMesh();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		return arrMesh;
	}
}
