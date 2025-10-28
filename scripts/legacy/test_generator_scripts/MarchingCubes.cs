using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

// lookup table: https://gist.github.com/dwilliamson/c041e3454a713e58baf6e4f8e5fffecd

public partial class MarchingCubes : Node3D
{
	[Export] int sizeX = 16, sizeY = 16, sizeZ = 16;


	[Export] float frequency = 0.02f, scale = 20f, fudgeFactor = 1.2f, sharpenator = 1.2f;

	public Vector3I offset = new Vector3I(0,0,0);

	FastNoiseLite noise = new FastNoiseLite();
	int[,,] noiseMatrix;

	public override void _Ready()
	{
		GenerateNoise();
		//GenerateCubes();
		GenerateMesh();
	}

	private void GenerateNoise()
	{
		noiseMatrix = new int[sizeX + 1, sizeY + 1, sizeZ + 1];

		RandomNumberGenerator rng = new RandomNumberGenerator();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		noise.Seed = 3564;
		noise.Frequency = 0.1f;
		noise.FractalOctaves = 4;
		noise.FractalLacunarity = 2f;
		noise.FractalGain = 0.5f;

		for (int x = 0; x <= sizeX; x++)
		{
			for (int z = 0; z <= sizeZ; z++)
			{
				float worldX = (x + offset.X) * frequency;
				float worldZ = (z + offset.Z) * frequency;

				float n = noise.GetNoise2D(worldX, worldZ);
				n += 0.5f * noise.GetNoise2D(worldX * 2, worldZ * 2);
				n += 0.25f * noise.GetNoise2D(worldX * 4, worldZ * 4);

				float heightValue = n / 1.75f;

				// set between 0 and 1
				heightValue = (heightValue + 1) / 2;
				heightValue = MathF.Pow(heightValue, sharpenator);

				int cutoff = (int)(heightValue * sizeY);

				for (int y = 0; y <= cutoff; y++)
				{
					noiseMatrix[x, y, z] = 1;
				}
			}
		}
	}

	private void GenerateMesh()
	{
		Godot.Collections.Array surfaceArray = [];
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		List<Vector3> verts = []; // vector positions in 3D space
		List<Vector2> uvs = []; // UV coordinates for texturing
		List<Vector3> normals = []; // Normal vectors for lighting calculations
		List<int> indices = []; // Triangle list
		List<Color> colors = new List<Color>();

		// -- GENERATION --

		for (int x = 0; x < sizeX; x++)
		{
			for (int y = 0; y < sizeY; y++)
			{
				for (int z = 0; z < sizeZ; z++)
				{
					Vector3[] cubeCorners = new Vector3[8];
					// for the lookup table use, go in a line across starting with bottom front, up, then same for back

					// front
					cubeCorners[0] = new Vector3(x, y, z); // left
					cubeCorners[1] = new Vector3(x + 1, y, z); // right
					cubeCorners[2] = new Vector3(x, y + 1, z); // up-left
					cubeCorners[3] = new Vector3(x + 1, y + 1, z); // up-right
																   // back
					cubeCorners[4] = new Vector3(x, y, z + 1); // back-left
					cubeCorners[5] = new Vector3(x + 1, y, z + 1); // back-right
					cubeCorners[6] = new Vector3(x, y + 1, z + 1); // back-left-up
					cubeCorners[7] = new Vector3(x + 1, y + 1, z + 1); // back-right-up

					// find values
					int[] cornerValues = new int[8];
					for (int i = 0; i < 8; i++)
					{
						Vector3 pos = cubeCorners[i];
						cornerValues[i] = noiseMatrix[(int)pos.X, (int)pos.Y, (int)pos.Z];
					}

					// determine index of case, assume 0 isoLevel
					int cubeIndex = 0;
					for (int i = 0; i < 8; i++)
					{
						if (cornerValues[i] > 0)
						{
							cubeIndex |= 1 << i;
						}
					}

					// Case finding
					int edges = MarchingCubesLookup.edgeMasks[cubeIndex];
					int[] tris = MarchingCubesLookup.TriangleTable[cubeIndex];

					// generating vertexes
					Vector3[] vertList = new Vector3[12];
					for (int i = 0; i < 12; i++)
					{
						if ((edges & (1 << i)) != 0)
						{
							int a0 = MarchingCubesLookup.EdgeVertexIndices[i, 0];
							int b0 = MarchingCubesLookup.EdgeVertexIndices[i, 1];
							vertList[i] = VertextInterpolate(
								0f, // isoLevel
								cubeCorners[a0],
								cubeCorners[b0],
								cornerValues[a0],
								cornerValues[b0]
							);
						}
					}

					// finally generating triangles
					for (int t = 0; t < tris.Length; t += 3)
					{
						if (tris[t] == -1) break;

						Vector3 v0 = vertList[tris[t]];
						Vector3 v1 = vertList[tris[t + 1]];
						Vector3 v2 = vertList[tris[t + 2]];

						int indexStart = verts.Count;

						verts.Add(v0);
						verts.Add(v1);
						verts.Add(v2);

						Vector3 normal = (v1 - v0).Cross(v2 - v0).Normalized();
						normals.Add(normal);
						normals.Add(normal);
						normals.Add(normal);

						indices.Add(indexStart + 2);
						indices.Add(indexStart + 1);
						indices.Add(indexStart);

						uvs.Add(new Vector2(v0.X, v0.Z));
						uvs.Add(new Vector2(v1.X, v1.Z));
						uvs.Add(new Vector2(v2.X, v2.Z));

						float averageY = (v0.Y + v1.Y + v2.Y) / 3;
						Color col;

						if (averageY > 18)
						{
							col = new Color(0.8f, 0.8f, 0.8f);
						}
						else if (averageY > 14)
						{
							float s = Mathf.InverseLerp(14f, 18f, averageY);
							col = new Color(0.2f, 0.2f, 0.2f).Lerp(new Color(0.8f, 0.8f, 0.8f), s);
						}
						else if (averageY > 7)
						{
							float s = Mathf.InverseLerp(7f, 14f, averageY);
							col = new Color(0.25f, 0.75f, 0f).Lerp(new Color(0.2f, 0.2f, 0.2f), s);
						}
						else
						{
							float s = Mathf.InverseLerp(2f, 7f, averageY);
							col = new Color(0f, 0.2f, 0f).Lerp(new Color(0.25f, 0.75f, 0f), s);
						}

						colors.Add(col);
						colors.Add(col);
						colors.Add(col);
					}

				}
			}
		}

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();

		MeshInstance3D meshInstance = new MeshInstance3D();
		ArrayMesh arrMesh = new ArrayMesh();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		meshInstance.Mesh = arrMesh;

		StandardMaterial3D mat = new StandardMaterial3D();
		mat.VertexColorUseAsAlbedo = true; 
		mat.VertexColorIsSrgb = false;
		meshInstance.MaterialOverride = mat;

		AddChild(meshInstance);
	}


	private Vector3 MidPoint(Vector3 p1, Vector3 p2)
	{
		return (p1 + p2) * 0.5f;
	}

	// use later
	private Vector3 VertextInterpolate(float isoLevel, Vector3 p1, Vector3 p2, float valp1, float valp2)
	{
		if (Mathf.Abs(isoLevel - valp1) < 0.00001f) return p1;
		if (Mathf.Abs(isoLevel - valp2) < 0.00001f) return p2;
		if (Mathf.Abs(valp1 - valp2) < 0.00001f) return p1;
		float mu = (isoLevel - valp1) / (valp2 - valp1);
		return p1 + mu * (p2 - p1);
	}
}