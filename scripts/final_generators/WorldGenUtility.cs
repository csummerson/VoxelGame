using Godot;
using System;
using System.Collections.Specialized;
using System.Drawing;

public static class WorldGenUtility
{
    public const int chunkSize = 16;
    public const int chunkHeight = 256;


    // Array references
    public static Vector3[] faceDirections =
    {
        Vector3.Back, Vector3.Right, Vector3.Forward,
        Vector3.Left, Vector3.Up, Vector3.Down
    };

    public static Vector3[] faceRotations =
    {
        new Vector3(90, 0, 0), new Vector3(90, 90, 0), new Vector3(90, 180, 0),
        new Vector3(90, 270, 0), Vector3.Zero, Vector3.Zero
    };

    public static Basis reflectionBasis = new Basis(
        new Vector3(1, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 0, -1)
    );

    public static Vector3I[] cornerOffsets = new Vector3I[]
    {
        new Vector3I(0, 0, 0), // i = 0 -> (000)
        new Vector3I(1, 0, 0), // i = 1 -> (001)
        new Vector3I(0, 1, 0), // i = 2 -> (010)
        new Vector3I(1, 1, 0), // i = 3 -> (011)
        new Vector3I(0, 0, 1), // i = 4 -> (100)
        new Vector3I(1, 0, 1), // i = 5 -> (101)
        new Vector3I(0, 1, 1), // i = 6 -> (110)
        new Vector3I(1, 1, 1), // i = 7 -> (111)
    };

    public static readonly int[,] edgePairs = new int[,] {
        {0,1},{1,3},{3,2},{2,0},
        {4,5},{5,7},{7,6},{6,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    public static readonly Vector2I[] chunkNeighbors = new Vector2I[]
    {
        new (0,0), new (1, 0), new (0, 1), new (1, 1),
        new (-1, 0), new (0, -1), new (-1, -1), new (-1, 1), new (1, -1)
    };

    // Functions
    public static int GetFaceIndex(Vector3 worldPosition)
    {
        Vector3 absolute = worldPosition.Abs();
        Vector3.Axis maxAxis = absolute.MaxAxisIndex();
        int axisSign = Mathf.Sign(worldPosition[(int)maxAxis]);

        return (maxAxis, axisSign) switch
        {
            (Vector3.Axis.Z, 1) => 0,
            (Vector3.Axis.X, 1) => 1,
            (Vector3.Axis.Z, -1) => 2,
            (Vector3.Axis.X, -1) => 3,
            (Vector3.Axis.Y, 1) => 4,
            (Vector3.Axis.Y, -1) => 5,
            _ => 0
        };
    }

    // Takes a world coodrinate combined with a face index and returns the Vector2 chunk coordinate on the face
    public static Vector2I SphereToFaceChunk(Vector3 position, int faceIndex, int chunkRadius)
    {
        float height = position.Length();
        Vector3 norm = position / height;

        switch (faceIndex)
        {
            case 0: // +Z
                Vector2I unitVector = InverseMap(position.X, position.Y);
                return unitVector * chunkRadius;

            case 1: // +X
                unitVector = InverseMap(-position.Z, position.Y);
                return unitVector * chunkRadius;

            case 2: // -Z
                unitVector = InverseMap(-position.X, position.Y);
                return unitVector * chunkRadius;

            case 3: // -X
                unitVector = InverseMap(position.Z, position.Y);
                return unitVector * chunkRadius;

            case 4: // +Y
                unitVector = InverseMap(position.X, -position.Z);
                return unitVector * chunkRadius;

            case 5: // -Y
                unitVector = InverseMap(position.X, position.Z);
                return unitVector * chunkRadius;

            default:
                return Vector2I.Zero;
        }
    }

    // Determines LOD based on distance from player and given thresholds
    public static int CalculateLOD(int distance, int[] lodThresholds)
    {
        for (int i = 0; i < lodThresholds.Length; i++)
        {
            if (distance <= lodThresholds[i])
                return (int)Mathf.Pow(2, i);
        }
        return (int)Mathf.Pow(2, lodThresholds.Length);
    }
    
    public static int LODIndex(int distance, int[] lodThresholds)
    {
        for (int i = 0; i < lodThresholds.Length; i++)
        {
            if (distance <= lodThresholds[i])
                return i;
        }
        return 0;
    }

    // Maps a local cube position to the world sphere position
    public static Vector3 CubeToSphere(Vector3 localPosition, Transform3D basis, float radius)
    {
        // grab y level (determines shell)
        float yLevel = localPosition.Y;

        // Transform to world space
        Vector3 worldPosition = basis * localPosition;

        // Normalize the position 
        // XZ becomes constrained to [-1,1]
        worldPosition /= radius;

        // Place local y onto the surface of the local unit cube
        float absX = Mathf.Abs(worldPosition.X);
        float absY = Mathf.Abs(worldPosition.Y);
        float absZ = Mathf.Abs(worldPosition.Z);

        if (absX >= absY && absX >= absZ)
        {
            // Y on X face
            worldPosition.X = worldPosition.X > 0 ? 1 : -1;
        }
        else if (absY >= absX && absY >= absZ)
        {
            // Y on Y face
            worldPosition.Y = worldPosition.Y > 0 ? 1 : -1;
        }
        else
        {
            // Y on Z face
            worldPosition.Z = worldPosition.Z > 0 ? 1 : -1;
        }

        // Apply mapping formula
        float x2 = localPosition.X * localPosition.X;
        float y2 = localPosition.Y * localPosition.Y;
        float z2 = localPosition.Z * localPosition.Z;

        float x = localPosition.X * Mathf.Sqrt(1 - (y2 / 2) - (z2 / 2) + (y2 * z2 / 3));
        float y = localPosition.Y * Mathf.Sqrt(1 - (z2 / 2) - (x2 / 2) + (z2 * x2 / 3));
        float z = localPosition.Z * Mathf.Sqrt(1 - (x2 / 2) - (y2 / 2) + (x2 * y2 / 3));

        // Scale it up to sphere shell
        Vector3 mapping = new Vector3(x, y, z) * (radius + yLevel);

        // Return sphere position as world coordinate
        return mapping;
    }

    // Takes a world positin and returns an appropriate chunk position
    public static Vector2I WorldToChunk(Vector3 position)
    {
        int x = (int)position.X / chunkSize;
        int y = (int)position.Z / chunkSize;
        return new Vector2I(x, y);
    }

    public static Vector2I WorldToLod(Vector3 position, int lod)
    {
        int scale = chunkSize * lod;
        int x = Mathf.FloorToInt(position.X / scale);
        int y = Mathf.FloorToInt(position.Z / scale);
        return new Vector2I(x, y);
    }


    // Helper methods
    // adapted from: https://stackoverflow.com/a/2698997
    private static Vector2I InverseMap(float u, float v)
    {
        float inverseSqrt2 = 0.70710676908493042f;

        float a2 = u * u * 2;
        float b2 = v * v * 2;
        float inner = -a2 + b2 - 3;
        float innerSqrt = -Mathf.Sqrt((inner * inner) - 12f * a2);

        if (u != 0f)
        {
            float val = innerSqrt + a2 - b2 + 3f;
            u = Mathf.Sign(u) * Mathf.Sqrt(Mathf.Max(val, 0f)) * inverseSqrt2;
        }

        if (v != 0f)
        {
            float val = innerSqrt - a2 + b2 + 3f;
            v = Mathf.Sign(v) * Mathf.Sqrt(Mathf.Max(val, 0f)) * inverseSqrt2;
        }

        u = Mathf.Clamp(u, -1f, 1f) + 1;
        v = Mathf.Clamp(v, -1f, 1f) + 1; // shift array to load from 0 to 2

        return new Vector2I((int)u, (int)v);
    }
}
