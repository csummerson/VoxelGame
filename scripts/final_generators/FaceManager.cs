using Godot;
using System;

public partial class FaceManager : Node3D
{
    private int faceID;
    private int planetChunkRadius;
    private Vector3 localUp;

    public FaceManager(int faceID, int planetChunkRadius, Vector3 localUp)
    {
        this.faceID = faceID;
        this.planetChunkRadius = planetChunkRadius;
        this.localUp = localUp;
    }

    public override void _Ready()
    {
        Position = localUp * planetChunkRadius * WorldGenUtility.chunkSize;
        RotationDegrees = WorldGenUtility.faceRotations[faceID];

        if (faceID == 5)
        {
            Transform3D xf = GlobalTransform;
            xf.Basis = WorldGenUtility.reflectionBasis * xf.Basis;
            GlobalTransform = xf;
        }
    }

    public ChunkManager LoadChunkAsync(Vector2I coordinate, int resolution = 1, bool generateCollider = true)
    {
        //ChunkManager chunk = new ChunkManager();
        return null;
    }
}
