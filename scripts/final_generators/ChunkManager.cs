using Godot;
using System;
using System.Threading.Tasks;

public partial class ChunkManager : Node3D
{
    int resolution;
    bool loadCollider;

    private ChunkData chunkData;
    private ChunkMesher chunkMesher;

    public event Action<ChunkManager> OnChunkLoaded;

    public ChunkManager(int resolution, bool loadCollider)
    {
        this.resolution = resolution;
        this.loadCollider = loadCollider;
    }

    public async void StartLoadingAsync(TerrainField terrainField)
    {
        Vector3 positionSnapshot = Vector3.Zero;
        
        try {
            positionSnapshot = Position;
        } catch (ObjectDisposedException) {
            NotifyLoaded();
            return;
        }

        var meshData = await Task.Run(() =>
        {
            chunkData = new ChunkData(terrainField);
            chunkData.GenerateData(positionSnapshot, resolution);

            chunkMesher = new ChunkMesher(chunkData, resolution, loadCollider);
            return chunkMesher.GenerateMesh();
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
        chunkData.Deform(localPoint, radius, delta);
        RebuildMesh();
    }
    
    private async void RebuildMesh()
    {
        if (chunkMesher == null || chunkData == null)
            return;

        var meshData = await Task.Run(() => chunkMesher.GenerateMesh());
        CallDeferred(nameof(ApplyMesh), meshData);
    }


    private void ApplyMesh(ArrayMeshData meshData)
    {
        // kill the children
        foreach (var child in GetChildren())
            child.QueueFree();

        ArrayMesh arrayMesh = new ArrayMesh();
        int surfaceIndex = 0;

        foreach (var kvp in meshData.materialIndices)
        {
            byte materialId = kvp.Key;
            var indices = kvp.Value;

            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            if (MaterialLibrary.Materials.ContainsKey(materialId))
                arrayMesh.SurfaceSetMaterial(surfaceIndex, MaterialLibrary.Materials[materialId]);

            surfaceIndex++;
        }

        if (loadCollider)
        {
            // Body object to interact with physics
            StaticBody3D body = new StaticBody3D();
            AddChild(body);

            // Collider mesh
            CollisionShape3D collider = new CollisionShape3D();
            ConcavePolygonShape3D colliderMesh = new ConcavePolygonShape3D();
            colliderMesh.SetFaces(meshData.collisionVerts.ToArray());
            collider.Shape = colliderMesh;
            body.AddChild(collider);
        }

        MeshInstance3D chunkMesh = new MeshInstance3D();
        chunkMesh.Mesh = arrayMesh;

        AddChild(chunkMesh);
    }
    
    private void NotifyLoaded()
    {
        OnChunkLoaded?.Invoke(this);
    }
}
