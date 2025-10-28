using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkMesher 
{
    private bool surfaceNets = true;
    private int resolution = 1;
    private bool loadCollider = false;
    ChunkData chunkData;

    public ChunkMesher(ChunkData data, int resolution, bool loadCollider)
    {
        this.chunkData = data;
        this.resolution = resolution;
        this.loadCollider = loadCollider;
    }

    public ArrayMeshData GenerateMesh()
    {
        int chunkWidth = WorldGenUtility.chunkSize;
        int chunkHeight = WorldGenUtility.chunkHeight;

        ArrayMeshData meshData = new ArrayMeshData(chunkWidth + 2, chunkHeight + 2, chunkWidth + 2);

        // Add + 2 to make skirts
        int pointWidth = chunkWidth + 1;
        int pointHeight = chunkHeight + 0;

        // Add + 1 to make skirts
        int faceWidth = chunkWidth + 1;
        int faceHeight = chunkHeight + 0;    

        // Vertext placement
        for (int x = 0; x < pointWidth; x++)
        {
            for (int y = 0; y < pointHeight; y++)
            {
                for (int z = 0; z < pointWidth; z++)
                {
                    Vector3 vertex = FindVertex(new Vector3I(x, y, z));
                    meshData.vertsList.Add(vertex);
                    meshData.vertIndicesAr[x, y, z] = meshData.vertsList.Count - 1;
                }
            }
        }

        // Mesh construction
        for (int x = 1; x < faceWidth; x++)
        {
            for (int y = 1; y < faceHeight; y++)
            {
                for (int z = 1; z < faceWidth; z++)
                {
                    // X faces	
                    bool solidX1 = chunkData.samples[x + 0, y, z] > 0;
                    bool solidX2 = chunkData.samples[x + 1, y, z] > 0;
                    if (solidX1 != solidX2)
                    {
                        int v0 = meshData.vertIndicesAr[x, y - 1, z - 1];
                        int v1 = meshData.vertIndicesAr[x, y - 0, z - 1];
                        int v2 = meshData.vertIndicesAr[x, y - 0, z - 0];
                        int v3 = meshData.vertIndicesAr[x, y - 1, z - 0];

                        byte mat = solidX2 ? chunkData.materials[x + 1, y, z] : chunkData.materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, solidX2, meshData, mat);
                    }

                    // Y faces
                    bool solidY1 = chunkData.samples[x, y + 0, z] > 0;
                    bool solidY2 = chunkData.samples[x, y + 1, z] > 0;

                    if (solidY1 != solidY2)
                    {
                        int v0 = meshData.vertIndicesAr[x - 1, y, z - 1];
                        int v1 = meshData.vertIndicesAr[x - 0, y, z - 1];
                        int v2 = meshData.vertIndicesAr[x - 0, y, z - 0];
                        int v3 = meshData.vertIndicesAr[x - 1, y, z - 0];

                        byte mat = solidY2 ? chunkData.materials[x, y + 1, z] : chunkData.materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, !solidY2, meshData, mat);
                    }

                    // Z faces
                    bool solidZ1 = chunkData.samples[x, y, z + 0] > 0;
                    bool solidZ2 = chunkData.samples[x, y, z + 1] > 0;
                    if (solidZ1 != solidZ2)
                    {
                        int v0 = meshData.vertIndicesAr[x - 1, y - 1, z];
                        int v1 = meshData.vertIndicesAr[x - 0, y - 1, z];
                        int v2 = meshData.vertIndicesAr[x - 0, y - 0, z];
                        int v3 = meshData.vertIndicesAr[x - 1, y - 0, z];

                        byte mat = solidZ2 ? chunkData.materials[x, y, z + 1] : chunkData.materials[x, y, z];
                        AddQuad(v0, v1, v2, v3, solidZ2, meshData, mat);
                    }
                }
            }
        }

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
        meshData.normals.Add(normal);
        meshData.normals.Add(normal);
        meshData.normals.Add(normal);

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
        if (!surfaceNets)
        {
            return (pos + Vector3.One * 0.5f) * resolution;
        }

        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < 12; i++)
        {
            var edge1 = WorldGenUtility.edgePairs[i, 0];
            var edge2 = WorldGenUtility.edgePairs[i, 1];

            var corn1 = WorldGenUtility.cornerOffsets[edge1];
            var corn2 = WorldGenUtility.cornerOffsets[edge2];

            Vector3 a = pos + corn1;
            Vector3 b = pos + corn2;

            float valA = chunkData.samples[(int)a.X, (int)a.Y, (int)a.Z];
            float valB = chunkData.samples[(int)b.X, (int)b.Y, (int)b.Z];

            if ((valA > 0) != (valB > 0))
            {
                Vector3 posInter = a + (b - a) * (valA / (valA - valB)); // linear interp
                positions.Add(posInter * resolution);
            }
        }

        if (positions.Count == 0)
        {
            return (pos + Vector3.One * 0.5f) * resolution;
        }
        return Centroid(positions);
    }
    
    Vector3 Centroid(List<Vector3> positions)
    {
        Vector3 sum = Vector3.Zero;

        foreach (Vector3 p in positions) {
            sum += p;
        }

        return sum / positions.Count;
    }
}
