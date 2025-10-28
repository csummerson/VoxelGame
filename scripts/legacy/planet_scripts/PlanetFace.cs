using Godot;
using System;

public partial class PlanetFace : Node3D
{
    int chunkRadius;
    Vector3 localUp;

    FaceChunk[,] chunks;

    CubeFace faceID;

    public PlanetFace(Vector3 localUp, int chunkRadius, CubeFace faceID)
    {
        this.localUp = localUp;
        this.chunkRadius = chunkRadius;
        this.faceID = faceID;
        chunks = new FaceChunk[chunkRadius * 2 + 1, chunkRadius * 2 + 1];
    }

    public override void _Ready()
    {
        Position = localUp * chunkRadius * WorldGenUtilities.chunkSize;

        Vector3 localRight = new Vector3(localUp.Y, localUp.Z, localUp.X);
        Vector3 localForward = -localUp.Cross(localRight); // godot sucks!
        Basis basis = new Basis(localRight, localUp, localForward);

        Transform3D transform = Transform3D.Identity;
        transform.Basis = basis;
        transform.Origin = Position;
        Transform = transform;
    }


    public FaceChunk LoadChunk(Vector2I coordinate)
    {
        GD.Print(coordinate);

        FaceChunk chunk = new FaceChunk();
        chunks[coordinate.X + chunkRadius, coordinate.Y + chunkRadius] = chunk;
        chunk.blockRadius = 16 * chunkRadius;
        chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(coordinate.X, 0, coordinate.Y));
        chunk.localBasis = Transform;
        //chunk.adaptive = adaptive;
        AddChild(chunk);
        chunk.Generate();

        //chunks[coordinate.X + chunkRadius, coordinate.Y + chunkRadius] = chunk;
        return chunk;
    }

    public FaceChunk LoadChunkAsync(Vector2I coordinate, bool generateCollider = false)
    {
        FaceChunk chunk = new FaceChunk();
        chunks[coordinate.X + chunkRadius, coordinate.Y + chunkRadius] = chunk;
        chunk.blockRadius = 16 * chunkRadius;
        chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(coordinate.X, 0, coordinate.Y));
        chunk.localBasis = Transform;
        AddChild(chunk);

        chunk.GenerateAsync((meshData) =>
        {
            // Update to assemble meshes here

            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = meshData.verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = meshData.normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = meshData.indices.ToArray();

            ArrayMesh arrMesh = new ArrayMesh();
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

            MeshInstance3D meshInstance = new MeshInstance3D();
            meshInstance.Mesh = arrMesh;

            StandardMaterial3D mat = new StandardMaterial3D();
            mat.VertexColorUseAsAlbedo = true;
            mat.VertexColorIsSrgb = false;
            meshInstance.MaterialOverride = mat;

            chunk.AddChild(meshInstance);
            
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

    public void LoadAllChunks()
    {
        for (int x = -chunkRadius; x < chunkRadius; x++)
        {
            for (int z = -chunkRadius; z < chunkRadius; z++)
            {
                FaceChunk chunk = new FaceChunk();
                chunk.blockRadius = 16 * chunkRadius;
                chunk.Position = WorldGenUtilities.ChunkToWorld(new Vector3(x, 0, z));
                chunk.localBasis = Transform;
                //chunk.adaptive = adaptive;
                AddChild(chunk);
                chunk.Generate();
                //MeshInstance3D mesh = chunk.GetChild<MeshInstance3D>(0);
                //StandardMaterial3D mat = new StandardMaterial3D();
                //mat.AlbedoColor = WorldGenUtilities.debugColors[faceNum];
                //mesh.MaterialOverride = mat;
                chunks[x + chunkRadius, z + chunkRadius] = chunk;
            }
        }
    }
}
