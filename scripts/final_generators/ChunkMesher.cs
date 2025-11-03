using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public partial class ChunkMesher 
{
    private bool useSurfaceNets = true;
    private int resolution = 1;
    private bool loadCollider = false;
    private float[,,] samples;
    private byte[,,] materials;

    private ThreadLocal<Vector3[]> edgeBuffer = new ThreadLocal<Vector3[]>(() => new Vector3[12]);

    public ChunkMesher(int resolution, bool loadCollider, bool useSurfaceNets)
    {
        this.resolution = resolution;
        this.loadCollider = loadCollider;
        this.useSurfaceNets = useSurfaceNets;
    }

    public ArrayMeshData GenerateMesh(ChunkData data)
    {
        samples = data.samples;
        materials = data.materials;
        
        //Stopwatch stopwatch = Stopwatch.StartNew();

        int chunkWidth = WorldGenUtility.chunkSize;
        int chunkHeight = WorldGenUtility.chunkHeight;

        int pointWidth = chunkWidth + 1;
        int pointHeight = chunkHeight + 0;

        ArrayMeshData meshData = new ArrayMeshData(pointWidth, pointHeight, pointWidth);

        // Vertext placement
        for (int x = 0; x < pointWidth; x++)
        {
            for (int y = 0; y < pointHeight; y++)
            {
                for (int z = 0; z < pointWidth; z++)
                {
                    Vector3 vertex = FindVertex(new Vector3I(x, y, z));
                    int index = meshData.LinearIndex(x, y, z);
                    meshData.vertsList[index] = vertex;
                    meshData.vertIndicesAr[x, y, z] = index;
                }
            }
        }

        // Mesh construction
        for (int x = 1; x < pointWidth; x++)
        {
            for (int y = 1; y < pointHeight; y++)
            {
                for (int z = 1; z < pointWidth; z++)
                {
                    // Exit out
                    int index = meshData.LinearIndex(x, y, z);
                    if (float.IsNaN(meshData.vertsList[index].X)) continue;
                    
                    // X faces	
                    bool solidX1 = samples[x, y, z] > 0;
                    bool solidX2 = samples[x + 1, y, z] > 0;
                    if (solidX1 != solidX2)
                    {
                        int v0 = meshData.vertIndicesAr[x, y - 1, z - 1];
                        int v1 = meshData.vertIndicesAr[x, y - 0, z - 1];
                        int v2 = meshData.vertIndicesAr[x, y - 0, z - 0];
                        int v3 = meshData.vertIndicesAr[x, y - 1, z - 0];

                        byte mat = solidX2 ? materials[x + 1, y, z] : materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, solidX2, meshData, mat);
                    }

                    // Y faces
                    bool solidY1 = samples[x, y, z] > 0;
                    bool solidY2 = samples[x, y + 1, z] > 0;
                    if (solidY1 != solidY2)
                    {
                        int v0 = meshData.vertIndicesAr[x - 1, y, z - 1];
                        int v1 = meshData.vertIndicesAr[x - 1, y, z - 0];
                        int v2 = meshData.vertIndicesAr[x - 0, y, z - 0];
                        int v3 = meshData.vertIndicesAr[x - 0, y, z - 1];

                        byte mat = solidY2 ? materials[x, y + 1, z] : materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, solidY2, meshData, mat);
                    }

                    // Z faces
                    bool solidZ1 = samples[x, y, z] > 0;
                    bool solidZ2 = samples[x, y, z + 1] > 0;
                    if (solidZ1 != solidZ2)
                    {
                        int v0 = meshData.vertIndicesAr[x - 1, y - 1, z];
                        int v1 = meshData.vertIndicesAr[x - 0, y - 1, z];
                        int v2 = meshData.vertIndicesAr[x - 0, y - 0, z];
                        int v3 = meshData.vertIndicesAr[x - 1, y - 0, z];

                        byte mat = solidZ2 ? materials[x, y, z + 1] : materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, solidZ2, meshData, mat);
                    }
                }
            }
        }

        //stopwatch.Stop();
        //GD.Print("GenerateMesh took: " + stopwatch.ElapsedMilliseconds + "ms");

        return meshData;
    }
    
    private void AddQuad(int a, int b, int c, int d, bool flip, ArrayMeshData meshData, byte mat)
    {
		if (flip)
		{
			AddTriangle(a, b, c, meshData, mat);
			AddTriangle(c, d, a, meshData, mat);
		}
		else
		{
			AddTriangle(c, b, a, meshData, mat);
			AddTriangle(a, d, c, meshData, mat);
		}
    }

    private void AddTriangle(int a, int b, int c, ArrayMeshData meshData, byte material)
    {
        int startIndex = meshData.verts.Count;

		Vector3 v0 = meshData.vertsList[a];
		Vector3 v1 = meshData.vertsList[b];
		Vector3 v2 = meshData.vertsList[c];

        meshData.verts.Add(v0);
        meshData.verts.Add(v1);
        meshData.verts.Add(v2);

		if (loadCollider)
		{
			meshData.collisionVerts.Add(v0);
			meshData.collisionVerts.Add(v1);
			meshData.collisionVerts.Add(v2);
		}

        Vector3 normal = (v2 - v0).Cross(v1 - v0).Normalized();
        meshData.vertexNormals.Add(normal);
        meshData.vertexNormals.Add(normal);
        meshData.vertexNormals.Add(normal);

        meshData.indices.Add(startIndex);
        meshData.indices.Add(startIndex + 1);
        meshData.indices.Add(startIndex + 2);

        if (!meshData.materialIndices.ContainsKey(material))
            meshData.materialIndices[material] = new List<int>();

        var list = meshData.materialIndices[material];
        list.Add(startIndex);
        list.Add(startIndex + 1);
        list.Add(startIndex + 2);
    }

    private Vector3 FindVertex(Vector3I pos)
    {
        if (!useSurfaceNets)
        {
            return (pos + Vector3.One * 0.5f) * resolution;
        }

        Vector3[] edgeHits = edgeBuffer.Value;
        int count = 0;

        for (int i = 0; i < 12; i++)
        {
            var edge1 = WorldGenUtility.edgePairs[i, 0];
            var edge2 = WorldGenUtility.edgePairs[i, 1];

            var corn1 = WorldGenUtility.cornerOffsets[edge1];
            var corn2 = WorldGenUtility.cornerOffsets[edge2];

            int ax = pos.X + (int)corn1.X;
            int ay = pos.Y + (int)corn1.Y;
            int az = pos.Z + (int)corn1.Z;

            int bx = pos.X + (int)corn2.X;
            int by = pos.Y + (int)corn2.Y;
            int bz = pos.Z + (int)corn2.Z;

            float valA = samples[ax, ay, az];
            float valB = samples[bx, by, bz];

            if ((valA > 0) != (valB > 0))
            {
                Vector3 a = new Vector3(ax, ay, az);
                Vector3 b = new Vector3(bx, by, bz);
                Vector3 posInter = a + (b - a) * (valA / (valA - valB));
                edgeHits[count++] = posInter * resolution;
            }
        }

        if (count == 0)
        {
            return new Vector3(float.NaN, float.NaN, float.NaN); // lets the mesher exit out early. Saved about 2 seconds at 16 chunks of render distance!
        }

        return Centroid(edgeHits, count);
    }
    
    Vector3 Centroid(Vector3[] positions, int count)
    {
        Vector3 sum = Vector3.Zero;

        for (int i = 0; i < count; i++)
        {
            sum += positions[i];
        }

        return sum / count;
    }
}
