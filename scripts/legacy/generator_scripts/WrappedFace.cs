using Godot;
using System;
using System.Collections.Generic;

public partial class WrappedFace : Node3D
{
    // indexed from top left, down, to bottom right

    int chunkRadius;
    int waterLevel;
    Vector3 localUp;
    int faceID;

    bool generateCollider = true;


    public WrappedFace(int chunkRadius, int waterLevel, Vector3 localUp, int face)
    {
        this.chunkRadius = chunkRadius;
        this.waterLevel = waterLevel;
        this.localUp = localUp;
        faceID = face;
    }

    public void GenerateAllChunks()
    {
        for (int x = 0; x < chunkRadius * 2; x++)
        {
            for (int y = 0; y < chunkRadius * 2; y++)
            {
                LoadChunkAsync(new Vector2I(x, y));
            }
        }
    }

    Dictionary<int, Vector3> rotationLookups = new()
    {
        {0, new Vector3(90, 0, 0)},
        {1, new Vector3(90, 90, 0)},
        {2, new Vector3(90, 180, 0)},
        {3, new Vector3(90, 270, 0)},
        {4, Vector3.Zero},
        {5, Vector3.Zero}
    };

    public override void _Ready()
    {
        Position = (localUp * chunkRadius * 16);
        RotationDegrees = rotationLookups[faceID];

        // reflect bottom face
        if (faceID == 5)
        {
            Transform3D xf = GlobalTransform;
            xf.Basis = WorldGenUtilities.reflection * xf.Basis;
            GlobalTransform = xf;
        }

        // Fix offset issue
        Transform3D transform = GlobalTransform;
        Vector3 localOffset = new Vector3(-0.5f, 0, 0);
        transform.Origin += transform.Basis * localOffset;
        GlobalTransform = transform;
    }

    public WrappedChunk LoadChunkAsync(Vector2I coordinate)
    {
        WrappedChunk chunk = new WrappedChunk();
        chunk.Name = coordinate.ToString();
        chunk.blockRadius = 16 * chunkRadius;
        chunk.generateCollider = generateCollider;

        int localY = (chunkRadius * 2 - 1) - coordinate.Y;

        chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(coordinate.X - chunkRadius, 0, localY - chunkRadius));
        chunk.localBasis = Transform;
        AddChild(chunk);

        chunk.GenerateAsync((meshData) =>
        {
            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = meshData.indices.ToArray();

            ArrayMesh arrayMesh = new ArrayMesh();
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            MeshInstance3D chunkMesh = new MeshInstance3D();
            chunkMesh.Mesh = arrayMesh;

            chunk.AddChild(chunkMesh);

            if (generateCollider)
            {
                // Body object to interact with physics
                StaticBody3D body = new StaticBody3D();
                chunk.AddChild(body);

                // Collider mesh
                CollisionShape3D collider = new CollisionShape3D();
                ConcavePolygonShape3D colliderMesh = new ConcavePolygonShape3D();
                colliderMesh.SetFaces(meshData.collisionVerts.ToArray());
                collider.Shape = colliderMesh;
                body.AddChild(collider);
            }
        });

        return chunk;
    }
    
    public WrappedChunk LoadChunkAsync(Vector2I coordinate, int res, bool loadCollider)
    {
        WrappedChunk chunk = new WrappedChunk();
        chunk.Name = coordinate.ToString();
        chunk.resolution = res;
        chunk.blockRadius = 16 * chunkRadius; 
        chunk.generateCollider = loadCollider;
        
        int localY = (chunkRadius * 2 - 1) - coordinate.Y;

        chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(coordinate.X - chunkRadius, 0, localY - chunkRadius));
        chunk.localBasis = Transform;
        AddChild(chunk);

        chunk.GenerateAsync((meshData) =>
        {
            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = meshData.indices.ToArray();

            ArrayMesh arrayMesh = new ArrayMesh();
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            MeshInstance3D chunkMesh = new MeshInstance3D();
            chunkMesh.Mesh = arrayMesh;

            chunk.AddChild(chunkMesh);

            if (loadCollider)
            {
                // Body object to interact with physics
                StaticBody3D body = new StaticBody3D();
                chunk.AddChild(body);

                // Collider mesh
                CollisionShape3D collider = new CollisionShape3D();
                ConcavePolygonShape3D colliderMesh = new ConcavePolygonShape3D();
                colliderMesh.SetFaces(meshData.collisionVerts.ToArray());
                collider.Shape = colliderMesh;
                body.AddChild(collider);
            }
        });

        return chunk;
    }
}
