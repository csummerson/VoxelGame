using Godot;
using System;

public class QuadtreeNode
{
    public Vector2 Position;
    public float Size;
    public int Level;
    public QuadtreeNode[] Children;
    public MeshInstance3D ChunkMesh;

    public bool IsLeaf => Children == null;

    public QuadtreeNode(Vector2 position, float size, int level)
    {
        Position = position;
        Size = size;
        Level = level;
    }

    public void Update(Vector3 playerPos, QuadManager manager, int maxLevel)
    {
        float distance = playerPos.DistanceTo(new Vector3(Position.X, 0, Position.Y));
        bool shouldSubdivide = distance < Size * 2f && Level < maxLevel;
        bool shouldMerge = distance > Size * 4f && !IsLeaf;

        if (shouldSubdivide && IsLeaf)
            Subdivide(manager);
        else if (shouldMerge && !IsLeaf)
            Merge(manager);

        if (!IsLeaf)
            foreach (var child in Children)
                child.Update(playerPos, manager, maxLevel);
    }

    public void Subdivide(QuadManager manager)
    {
        if (!IsLeaf) return;

        float half = Size / 2f;
        int nextLevel = Level + 1;
        Children = new QuadtreeNode[4];

        Children[0] = new QuadtreeNode(Position + new Vector2(-half / 2, -half / 2), half, nextLevel);
        Children[1] = new QuadtreeNode(Position + new Vector2(half / 2, -half / 2), half, nextLevel);
        Children[2] = new QuadtreeNode(Position + new Vector2(-half / 2, half / 2), half, nextLevel);
        Children[3] = new QuadtreeNode(Position + new Vector2(half / 2, half / 2), half, nextLevel);

        if (ChunkMesh != null)
            ChunkMesh.QueueFree();
        ChunkMesh = null;

        foreach (var child in Children)
            child.SpawnChunk(manager);
    }

    public void Merge(QuadManager manager)
    {
        if (IsLeaf) return;

        foreach (var child in Children)
            child.DestroyTile();

        Children = null;

        if (ChunkMesh == null)
            SpawnChunk(manager);
    }

    public void SpawnChunk(QuadManager manager)
    {
        ChunkMesh = manager.CreateChunkMesh(Position, Size, Level);
    }

    public void DestroyTile()
    {
        if (ChunkMesh != null)
            ChunkMesh.QueueFree();
        if (Children != null)
        {
            foreach (var child in Children)
                child.DestroyTile();
            Children = null;
        }
    }
}
