using Godot;
using System;
using System.Threading.Tasks;

public partial class ChunkData
{
    private TerrainField terrainField;
    private int chunkRadius;

    public float[,,] samples;
    public byte[,,] materials;
    private bool runParallel = false;

    // --- SPHERE TERRAIN ---
    public ChunkData(TerrainField terrainField, int chunkRadius)
    {
        this.terrainField = terrainField;
        this.chunkRadius = chunkRadius;
    }

    public void GenerateData(Vector3 position, Transform3D transform, int resolution = 1)
    {
        int chunkWidth = WorldGenUtility.chunkSize;
        int chunkHeight = WorldGenUtility.chunkHeight;

        int pointWidth = (chunkWidth + 1) / resolution;
        int pointHeight = (chunkHeight + 1) / resolution;

        samples = new float[pointWidth, pointHeight, pointWidth];

        if (runParallel)
        {
            Parallel.For(0, pointWidth / resolution, xi =>
            {
                int x = xi * resolution;
                for (int y = 0; y < pointHeight; y += resolution)
                {
                    for (int z = 0; z < pointWidth; z += resolution)
                    {
                        Vector3 samplePoint = position + new Vector3(x * resolution, y * resolution, z * resolution);
                        samples[x, y, z] = terrainField.SampleField(samplePoint, transform, chunkRadius * 16);
                    }
                }
            });
        }
        else
        {
            for (int x = 0; x < pointWidth; x += resolution)
            {
                for (int y = 0; y < pointHeight; y += resolution)
                {
                    for (int z = 0; z < pointWidth; z += resolution)
                    {
                        Vector3 samplePoint = position + new Vector3(x * resolution, y * resolution, z * resolution);
                        samples[x, y, z] = terrainField.SampleField(samplePoint, transform, chunkRadius * 16);
                    }
                }
            }
        }
    }

    // --- FLAT TERRAIN ---
    public ChunkData(TerrainField terrainField)
    {
        this.terrainField = terrainField;
    }

    public float[,,] GenerateData(Vector3 position, int resolution)
    {
        int chunkWidth = WorldGenUtility.chunkSize;
        int chunkHeight = WorldGenUtility.chunkHeight;

        int pointWidth = chunkWidth + 2;
        int pointHeight = chunkHeight + 1;

        samples = new float[pointWidth, pointHeight, pointWidth];
        materials = new byte[pointWidth, pointHeight, pointWidth];

        if (runParallel)
        {
            int slabSize = 8;
            Parallel.For(0, pointHeight / slabSize, yi =>
            {
                int yStart = yi * slabSize;
                int yEnd = Math.Min(yStart + slabSize, pointHeight);

                for (int y = yStart; y < yEnd; y++)
                {
                    for (int x = 0; x < pointWidth; x++)
                    {
                        for (int z = 0; z < pointWidth; z++)
                        {
                            Vector3 samplePoint = position + new Vector3(x * resolution, y * resolution, z * resolution);
                            float density = terrainField.SampleField(samplePoint);
                            samples[x, y, z] = density;

                            if (density > 0)
                            {
                                if (samplePoint.Y < 0)
                                    materials[x, y, z] = 1;
                                else if (samplePoint.Y < 64)
                                    materials[x, y, z] = 2;
                                else
                                    materials[x, y, z] = 3;
                            }
                            else
                            {
                                materials[x, y, z] = 0;
                            }
                        }
                    }
                }
            });
        }
        else
        {
            for (int x = 0; x < pointWidth; x++)
            {
                for (int y = 0; y < pointHeight; y++)
                {
                    for (int z = 0; z < pointWidth; z++)
                    {
                        Vector3 samplePoint = position + new Vector3(x * resolution, y * resolution, z * resolution);
                        float density = terrainField.SampleField(samplePoint);
                        samples[x, y, z] = density;

                        if (density > 0)
                        {
                            if (samplePoint.Y < 0)
                                materials[x, y, z] = 1; // stone
                            else if (samplePoint.Y < 60)
                                materials[x, y, z] = 2; // sand
                            else
                                materials[x, y, z] = 3; // grass
                        }
                        else
                        {
                            materials[x, y, z] = 0;
                        }
                    }
                }
            }
        }
        
        return samples;
    }

    public void Deform(Vector3 localPoint, int radius, float delta = -10f)
    {
        int width = samples.GetLength(0);
        int height = samples.GetLength(1);
        int depth = samples.GetLength(2);

        int centerX = Mathf.FloorToInt(localPoint.X);
        int centerY = Mathf.FloorToInt(localPoint.Y);
        int centerZ = Mathf.FloorToInt(localPoint.Z);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3I offset = new Vector3I(x, y, z);
                    if (offset.Length() > radius)
                        continue;

                    int vx = centerX + x;
                    int vy = centerY + y;
                    int vz = centerZ + z;

                    if (vx >= 0 && vy >= 0 && vz >= 0 &&
                        vx < width && vy < height && vz < depth)
                    {
                        samples[vx, vy, vz] += delta;
                        materials[vx, vy, vz] = 0;
                    }
                }
            }
        }
    }
}
