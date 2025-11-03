using Godot;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

public partial class ChunkData
{
    private TerrainField terrainField;
    public float[,,] samples;
    public byte[,,] materials;

    public bool parallel = false;

    // --- FLAT TERRAIN ---
    public ChunkData(TerrainField terrainField)
    {
        this.terrainField = terrainField;
    }

    public float[,,] GenerateData(Vector3 position, int resolution)
    {
        //Stopwatch stopwatch = Stopwatch.StartNew();
        
        int chunkWidth = WorldGenUtility.chunkSize;
        int chunkHeight = WorldGenUtility.chunkHeight;

        int pointWidth = chunkWidth + 2;
        int pointHeight = chunkHeight + 1;

        samples = new float[pointWidth, pointHeight, pointWidth];
        materials = new byte[pointWidth, pointHeight, pointWidth];

        if (parallel)
        {
            Parallel.For(0, pointWidth, x =>
            {
                float sx = position.X + x * resolution;
                for (int y = 0; y < pointHeight; y++)
                {
                    float sy = position.Y + y * resolution;
                    for (int z = 0; z < pointWidth; z++)
                    {
                        float sz = position.Z + z * resolution;

                        terrainField.SampleField(sx, sy, sz, out float density, out byte material);
                        
                        samples[x, y, z] = density;
                        materials[x, y, z] = material;
                    }
                }
            });
        } else
        {
            for (int x = 0; x < pointWidth; x++)
            {
                float sx = position.X + x * resolution;
                for (int y = 0; y < pointHeight; y++)
                {
                    float sy = position.Y + y * resolution;
                    for (int z = 0; z < pointWidth; z++)
                    {
                        float sz = position.Z + z * resolution;

                        terrainField.SampleField(sx, sy, sz, out float density, out byte material);
                        
                        samples[x, y, z] = density;
                        materials[x, y, z] = material;
                    }
                }
            }
        }

        //stopwatch.Stop();
        //GD.Print("GenerateData took: " + stopwatch.ElapsedMilliseconds + "ms");

        return samples;
    }
    
    public int Deform(Vector3 localPoint, int radius, float delta = -10f)
    {
        int dirty = 0;
        
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
                    if (offset.LengthSquared() > radius * radius)
                        continue;

                    int vx = centerX + x;
                    int vy = centerY + y;
                    int vz = centerZ + z;

                    if (vx >= 0 && vy >= 10 && vz >= 0 &&
                        vx < width && vy < height && vz < depth)
                    {
                        dirty = 1;
                        samples[vx, vy, vz] = delta;
                        materials[vx, vy, vz] = 0;
                    }
                }
            }
        }

        return dirty;
    }
}
