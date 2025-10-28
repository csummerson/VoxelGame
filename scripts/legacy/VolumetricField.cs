using Godot;
using System;

public partial class VolumetricField : Node3D
{
    private FastNoiseLite noise3D;
    private FastNoiseLite noise2D;

    private FastNoiseLite noise4D;

    private float baseHeight = 7f;
    private float terrainAmp = 2f;
    private float terrainFreq = 10f;

    private float yFrequency = 0.01f;


    private float caveAmp = 20f;
    private float caveFreq = 1f;
    private float epsilon = 0.001f;


    private int numLayers = 4;
    private float strength = 20;
    private float baseRoughness = 0.5f;
    private float roughness = 1;
    private float persistence = .5f;


    public int seed = 3564;

    public override void _Ready()
    {
        noise3D = new FastNoiseLite();
        noise3D.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise3D.Seed = seed;

        noise2D = new FastNoiseLite();
        noise2D.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise2D.Seed = seed;
    }

    public VolumetricField()
    {
        noise3D = new FastNoiseLite();
        noise3D.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        //noise3D.Seed = 3564;

        noise2D = new FastNoiseLite();
        noise2D.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        //noise2D.Seed = 3564;

    }

    Vector3 lastUp = Vector3.Zero;


    // Actually in use
    public float SampleValBasis(Vector3 position, Transform3D transform, float radius)
    {
        Vector3 spherePosition = WorldGenUtilities.PosOnSphere(position, radius, transform);
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
        
        return noiseValue * strength + baseHeight - position.Y;

        // float height = noise3D.GetNoise3Dv(spherePosition * terrainFreq) * terrainAmp;
        // return height + baseHeight - position.Y;
    }










    // Legacy
    public float SampleVal(Vector3 p, Vector3 localUp)
    {
        // value for the point itself
        Vector3 up = localUp.Normalized();
        Vector3 tangent = Vector3.Up.Cross(up);
        if (tangent.LengthSquared() < 0.001f)
            tangent = Vector3.Forward.Cross(up);
        tangent = tangent.Normalized();
        Vector3 bitangent = up.Cross(tangent);

        // Transform p into the local coordinate frame
        Vector3 localP = new Vector3(
            p.Dot(tangent),
            p.Dot(up),
            p.Dot(bitangent)
        );

        return noise3D.GetNoise3D(localP.X * terrainFreq, localP.Y * terrainFreq, localP.Z * terrainFreq);
    }

    private float Evaluate(Vector3 p, Vector3 localUp)
    {
        Vector3 up = localUp.Normalized();
        Vector3 tangent = Vector3.Up.Cross(up);
        if (tangent.LengthSquared() < 0.001f)
            tangent = Vector3.Forward.Cross(up);
        tangent = tangent.Normalized();
        Vector3 bitangent = up.Cross(tangent);

        // Transform p into the local coordinate frame
        Vector3 localP = new Vector3(
            p.Dot(tangent),
            p.Dot(up),
            p.Dot(bitangent)
        );

        return noise3D.GetNoise3D(localP.X * terrainFreq, localP.Y * terrainFreq, localP.Z * terrainFreq);
    }





    
    // Defunct
    public Vector3 SampleGrad(Vector3 position)
    {
        Vector3 ex = new Vector3(epsilon, 0, 0);
        Vector3 ey = new Vector3(0, epsilon, 0);
        Vector3 ez = new Vector3(0, 0, epsilon);

        float dx = (Evaluate(position + ex, Vector3.Up) - Evaluate(position - ex, Vector3.Up)) / (2 * epsilon);
        float dy = (Evaluate(position + ey, Vector3.Up) - Evaluate(position - ey, Vector3.Up)) / (2 * epsilon);
        float dz = (Evaluate(position + ez, Vector3.Up) - Evaluate(position - ez, Vector3.Up)) / (2 * epsilon);

        Vector3 gradient = new Vector3(dx, dy, dz).Normalized();

        return gradient;
    }
}
