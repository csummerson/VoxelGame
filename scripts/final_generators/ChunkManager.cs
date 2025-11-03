using Godot;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

public partial class ChunkManager : Node3D
{
    int resolution;
    bool loadCollider;

    private ChunkData chunkData;
    private ChunkMesher chunkMesher;

    public event Action<ChunkManager> OnChunkLoaded;

    public bool isLoaded = false;

    public bool useSurfaceNets;

    public ChunkManager(int resolution, bool loadCollider, bool useSurfaceNets)
    {
        this.resolution = resolution;
        this.loadCollider = loadCollider;
        this.useSurfaceNets = useSurfaceNets;
    }

    public async void CreateChunkAsync(TerrainField terrainField)
    {
        Vector3 positionSnapshot;

        try {
            positionSnapshot = Position;
            CreateWaterMesh();
        } catch (ObjectDisposedException) {
            NotifyLoaded();
            return;
        }

        ArrayMeshData meshData = await Task.Run(() =>
        {
            chunkData = new ChunkData(terrainField);
            chunkData.GenerateData(positionSnapshot, resolution);

            chunkMesher = new ChunkMesher(resolution, loadCollider, useSurfaceNets);
            return chunkMesher.GenerateMesh(chunkData);
        });

        try {
            CallDeferred(nameof(ApplyMesh), meshData);
            CallDeferred(nameof(NotifyLoaded));
        } catch (ObjectDisposedException) {
            NotifyLoaded();
            return;
        }
    }

    public void DeformGlobal(Vector3 globalPoint, int radius, float delta)
    {
        FlatTerrainManager manager = GetParent<FlatTerrainManager>();
        manager.ApplyDeform(globalPoint, radius, delta);
    }

    public void DeformLocal(Vector3 globalPoint, int radius, float delta)
    {
        Vector3 localPoint = ToLocal(globalPoint);
        int dirty = chunkData.Deform(localPoint, radius, delta);
        if (dirty == 1)
        {
            RebuildMesh();
        }
    }
    
    private async void RebuildMesh()
    {
        if (chunkData == null)
        {
            GD.PrintErr("No chunk data found.");
            return;
        }
        
        if (chunkMesher == null)
        {
            chunkMesher = new ChunkMesher(resolution, loadCollider, useSurfaceNets);
        }

        ArrayMeshData meshData = await Task.Run(() => chunkMesher.GenerateMesh(chunkData));
        CallDeferred(nameof(ApplyMesh), meshData);
    }


    private void ApplyMesh(ArrayMeshData meshData)
    {
        MeshInstance3D chunkMesh = GetNodeOrNull<MeshInstance3D>("Chunk Mesh");
        if (chunkMesh == null)
        {
            chunkMesh = new MeshInstance3D();
            chunkMesh.Name = "Chunk Mesh";
            AddChild(chunkMesh);
        }

        ArrayMesh arrayMesh = new ArrayMesh();
        int surfaceIndex = 0;

        foreach (var kvp in meshData.materialIndices)
        {
            byte materialId = kvp.Key;
            var indices = kvp.Value;

            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.vertexNormals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            if (MaterialLibrary.Materials.ContainsKey(materialId))
                arrayMesh.SurfaceSetMaterial(surfaceIndex, MaterialLibrary.Materials[materialId]);

            surfaceIndex++;
        }

        chunkMesh.Mesh = arrayMesh;

        // Collider stuff
        if (loadCollider)
        {
            // Body object to interact with physics
            StaticBody3D body = GetNodeOrNull<StaticBody3D>("Chunk Collider");
            if (body == null)
            {
                body = new StaticBody3D();
                body.Name = "Chunk Collider";
                AddChild(body);
            }

            // Collider mesh
            CollisionShape3D collider = body.GetNodeOrNull<CollisionShape3D>("Collision Shape");
            if (collider == null)
            {
                collider = new CollisionShape3D();
                collider.Name = "Collision Shape";
                body.AddChild(collider);
            }

            ConcavePolygonShape3D colliderMesh = new ConcavePolygonShape3D();
            colliderMesh.SetFaces(meshData.collisionVerts.ToArray());
            collider.Shape = colliderMesh;
        }

        meshData.Clear();
        chunkMesher = null; // gc collection?
    }

    private void CreateWaterMesh()
    {
        WaterGenerator water = new WaterGenerator(resolution);
        ArrayMesh waterArrMesh = water.GenerateMesh();
        MeshInstance3D waterMesh = new MeshInstance3D();
        waterMesh.Mesh = waterArrMesh;
        StandardMaterial3D waterMat = ResourceLoader.Load<StandardMaterial3D>("res://materials/other/water.tres");
        waterMesh.MaterialOverride = waterMat;
        waterMesh.Name = "Water";

        AddChild(waterMesh);
    }
    
    private void NotifyLoaded()
    {
        isLoaded = true;
        OnChunkLoaded?.Invoke(this);
    }
}
