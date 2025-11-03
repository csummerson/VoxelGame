using Godot;
using System;

public partial class TerrainField
{
    private FastNoiseLite terrainNoise;
    private FastNoiseLite caveNoise;

    private float baseHeight = 154f;

    private int numLayers = 4;
    private float strength = 40;
    private float baseRoughness = 0.5f;
    private float roughness = 1;
    private float persistence = .5f;

    private float caveFrequency = 0.01f;
    private float caveThreshold = 0.4f;
    private float caveStrength = 500f;

    private float maxNoiseSum;

    public TerrainField(int seed)
    {
        terrainNoise = new FastNoiseLite();
        terrainNoise.Seed = seed;
        terrainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

        caveNoise = new FastNoiseLite();
        caveNoise.Seed = seed;
        caveNoise.Frequency = caveFrequency;
        caveNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;

        float amplitude = 1f;
        float sum = 0f;
        for (int i = 0; i < numLayers; i++)
        {
            sum += amplitude;
            amplitude *= persistence;
        }
        maxNoiseSum = sum;
    }

    public void SampleField(float x, float y, float z, out float terrainDensity, out byte material)
    {
        // exit out of y stuff for now
        float maxPossible = baseHeight + (maxNoiseSum * strength) - y;
        if (maxPossible <= 0)
        {
            terrainDensity = -1f;
            material = 0;
            return;
        }
        
        // surface
        float noiseSum = 0;
        float frequency = baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < numLayers; i++)
        {
            noiseSum += (terrainNoise.GetNoise2D(x * frequency, z * frequency) + 1f) * 0.5f * amplitude;
            frequency *= roughness;
            amplitude *= persistence;
        }

        terrainDensity = noiseSum * strength + baseHeight - y;

        // caves
        if (terrainDensity > 0) 
        {
            float caveValue = caveNoise.GetNoise3D(x, y, z);
            
            if (caveValue > caveThreshold)
            {
                terrainDensity = -caveStrength * (caveValue - caveThreshold);
            }
        }

        if (y < 10)
        {
            terrainDensity += 500 / (y + 1);
        }

        // Material assignment
        if (terrainDensity > 0)
        {
            if (y < 10)
                material = 4; // mantleshell
            else if (y < 180)
                material = 1; // stone
            else if (y < 194)
                material = 2; // sand
            else
                material = 3; // grass
        }
        else
        {
            material = 0;
        }
    }
}
