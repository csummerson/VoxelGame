using Godot;
using System;

public partial class TerrainField
{
    private FastNoiseLite noise3D;

    private float baseHeight = 20f;


    private int numLayers = 4;
    private float strength = 40;
    private float baseRoughness = 0.5f;
    private float roughness = 1;
    private float persistence = .5f;


    public int seed = 3564;

    public TerrainField(int seed) {
        noise3D = new FastNoiseLite();
        noise3D.Seed = seed;
    }


    public float SampleField(Vector3 localPosition, Transform3D transform, float radius)
    {
        Vector3 spherePosition = WorldGenUtility.CubeToSphere(localPosition, transform, radius) * WorldGenUtility.chunkSize;
        float noiseValue = 0;
        float frequency = baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < numLayers; i++)
        {
            float v = noise3D.GetNoise3Dv(spherePosition * frequency);
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= roughness;
            amplitude *= persistence;
        }

        return noiseValue * strength + baseHeight - localPosition.Y;
    }

    public float SampleField(Vector3 worldPosition)
    {
        float noiseValue = 0;
        float frequency = baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < numLayers; i++)
        {
            float v = noise3D.GetNoise3Dv(worldPosition * frequency);
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= roughness;
            amplitude *= persistence;
        }
        return noiseValue * strength + baseHeight - worldPosition.Y;
    }
}
