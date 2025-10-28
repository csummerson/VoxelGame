using Godot;
using System;

public partial class PlanetController : Node3D
{
    [Export] public int chunkRadius = 16;
    [Export] public int renderDistance = 8;
    [Export] public int simulationDistance = 8;
    [Export] public int[] lodThresholds = { 8, 16, 32, 64 };
    [Export] public int seed = 3564;

    private FaceManager[] faces = new FaceManager[6];
    private PlanetTerrainManager terrainManager;

    public override void _EnterTree()
    {
        for (int i = 0; i < 6; i++)
        {
            FaceManager face = new FaceManager(i, chunkRadius, WorldGenUtility.faceDirections[i]);
            face.Name = "Face " + i.ToString();
            faces[i] = face;
            AddChild(face);
        }

        terrainManager = new PlanetTerrainManager(chunkRadius, faces, lodThresholds);
        AddChild(terrainManager);
    }

}
