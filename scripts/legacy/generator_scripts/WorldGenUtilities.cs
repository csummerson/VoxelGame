using Godot;
using System;

public static class WorldGenUtilities
{
	public const int chunkSize = 16;
	public const int chunkHeight = 256;

	public static Color[] debugColors = { Colors.White, Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple };

	public static Vector3[] directions = { Vector3.Back, Vector3.Right, Vector3.Forward, Vector3.Left, Vector3.Up, Vector3.Down };

	public static Basis reflection = new Basis(
		new Vector3(1, 0, 0),
		new Vector3(0, -1, 0),
		new Vector3(0, 0, -1)
	);

	public static Vector3I WorldToChunk(Vector3 position)
	{
		return new Vector3I(
			Mathf.FloorToInt(position.X / chunkSize),
			Mathf.FloorToInt(position.Y / chunkHeight),
			Mathf.FloorToInt(position.Z / chunkSize)
		);
	}

	public static Vector2I WorldToChunk(Vector2 position)
	{
		return new Vector2I(
			Mathf.FloorToInt(position.X / chunkSize),
			Mathf.FloorToInt(position.Y / chunkSize)
		);
	}

	public static Vector3 ChunkToWorld(Vector3 position)
	{
		return new Vector3(
			position.X * chunkSize,
			position.Y * chunkHeight,
			position.Z * chunkSize
		);
	}

	public static readonly int[,] edgePairs = new int[,] {
		{0,1},{1,3},{3,2},{2,0},
		{4,5},{5,7},{7,6},{6,4},
		{0,4},{1,5},{2,6},{3,7}
	};

	public static readonly (int x, int y, int z)[] cornerOffsets = new (int, int, int)[] {
		(0,0,0),(1,0,0),(0,1,0),(1,1,0),
		(0,0,1),(1,0,1),(0,1,1),(1,1,1)
	};
 
	/// <summary>
	/// Maps a position inside a cube to a sphere.
	/// </summary>
	/// <param name="position">Cube position</param>
	/// <param name="halfExtent">Half extent of cube</param>
	/// <param name="radius">Radius of resulting sphere (usually same as halfExtent)</param>
	/// <returns></returns>
	public static Vector3 MapToSphere(Vector3 position, float halfExtent, float radius)
	{
		float x2 = Mathf.Pow(position.X / halfExtent, 2);
		float y2 = Mathf.Pow(position.Y / halfExtent, 2);
		float z2 = Mathf.Pow(position.Z / halfExtent, 2);

		float x = position.X * Mathf.Sqrt(1 - (y2 / 2) - (z2 / 2) + (y2 * z2 / 3));
		float y = position.Y * Mathf.Sqrt(1 - (z2 / 2) - (x2 / 2) + (z2 * x2 / 3));
		float z = position.Z * Mathf.Sqrt(1 - (x2 / 2) - (y2 / 2) + (x2 * y2 / 3));

		return new Vector3(x, y, z) * radius;
	}

	public static Vector3 matching = Vector3.Zero;


	// The formula of fun (I hate myself)
	public static Vector2 SphereToFace(Vector3 pos, int face, int chunkRadius)
	{
		// fall onto surface
		float height = pos.Length();
		pos = pos / height;
		
		switch (face)
		{
			case 0: // +Z
				Vector2 unitVector = InverseMap(pos.X, pos.Y);
				return unitVector * chunkRadius;

			case 1: // +X
				unitVector = InverseMap(-pos.Z, pos.Y);
				return unitVector * chunkRadius;

		    case 2: // -Z
				unitVector = InverseMap(-pos.X, pos.Y);
				return unitVector * chunkRadius;

			case 3: // -X
				unitVector = InverseMap(pos.Z, pos.Y);
				return unitVector * chunkRadius;

			case 4: // +Y
				unitVector = InverseMap(pos.X, -pos.Z);
				return unitVector * chunkRadius;

			case 5: // -Y
				unitVector = InverseMap(pos.X, pos.Z);
				return unitVector * chunkRadius;

			default:
				return Vector2.Zero;
		}
	}

    // adapted from: https://stackoverflow.com/a/2698997
	private static Vector2 InverseMap(float u, float v)
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

		u = Mathf.Clamp(u, -1f, 1f);
		v = Mathf.Clamp(v, -1f, 1f);

		return new Vector2(u + 1, v + 1);
	}

	// Used to evaluate a player position on a cube face.
	public static Vector2I SphereToChunk(Vector3 worldPoint, CubeFace face, float radius)
	{
		Vector3 local = worldPoint.Normalized();

		switch (face)
		{
			case CubeFace.PosX: local = new Vector3(local.Z, 0, local.Y) / Mathf.Abs(local.X); break;
			case CubeFace.NegX: local = new Vector3(local.Z, 0, -local.Y) / Mathf.Abs(local.X); break;

			// Y cases are now correct
			case CubeFace.PosY: local = new Vector3(local.X, 0, local.Z) / Mathf.Abs(local.Y); break;
			case CubeFace.NegY: local = new Vector3(-local.X, 0, local.Z) / Mathf.Abs(local.Y); break;

			case CubeFace.PosZ: local = new Vector3(local.Y, 0, -local.X) / Mathf.Abs(local.Z); break;
			case CubeFace.NegZ: local = new Vector3(-local.Y, 0, -local.X) / Mathf.Abs(local.Z); break;
		}

		int chunksPerFace = Mathf.CeilToInt(2 * radius + 1);
		//GD.Print(chunksPerFace);
		int uChunk = Mathf.FloorToInt(local.X * 0.5f * chunksPerFace);
		int vChunk = Mathf.FloorToInt(local.Z * 0.5f * chunksPerFace);

		uChunk = (int) Mathf.Clamp(uChunk, -radius, radius);
		vChunk = (int) Mathf.Clamp(vChunk, -radius, radius);

		Vector2I chunkCoord = new Vector2I(uChunk, vChunk);
		//GD.Print(chunkCoord);

		return chunkCoord;
	}

	public static Vector3 PosOnSphere(Vector3 position, float radius, Transform3D basis)
	{
		// align inputs
		//position -= Vector3.One;

		// grab y level (determines shell)
		float yLevel = position.Y;

		// Transform to world space
		position = basis * position;

		// Normalize the position 
		// XZ becomes constrained to [-1,1]
		position /= radius;

		// Place y onto the surface of the local unit cube
		float absX = Mathf.Abs(position.X);
		float absY = Mathf.Abs(position.Y);
		float absZ = Mathf.Abs(position.Z);

		if (absX >= absY && absX >= absZ)
		{
			// X face
			position.X = position.X > 0 ? 1 : -1;
		}
		else if (absY >= absX && absY >= absZ)
		{
			// Y face
			position.Y = position.Y > 0 ? 1 : -1;
		}
		else
		{
			// Z face
			position.Z = position.Z > 0 ? 1 : -1;
		}

		// Apply mapping formula
		float x2 = position.X * position.X;
		float y2 = position.Y * position.Y;
		float z2 = position.Z * position.Z;

		float x = position.X * Mathf.Sqrt(1 - (y2 / 2) - (z2 / 2) + (y2 * z2 / 3));
		float y = position.Y * Mathf.Sqrt(1 - (z2 / 2) - (x2 / 2) + (z2 * x2 / 3));
		float z = position.Z * Mathf.Sqrt(1 - (x2 / 2) - (y2 / 2) + (x2 * y2 / 3));

		// Scale it up to sphere shell
		Vector3 mapping = new Vector3(x, y, z) * (radius + yLevel);

		// Undo cube offset
		//mapping += basis.Basis.Y * radius;

		// Return without basis change
		return mapping;
	}

	public static Vector3 MapToSphere(Vector3 position, float radius, Transform3D basis)
	{
		// align inputs
		//position -= Vector3.One;

		// grab y level (determines shell)
		float yLevel = position.Y;

		// Transform to cube space
		position = basis * position;

		// Normalize the position 
		// XZ becomes constrained to [-1,1]
		position /= radius;

		// Place y onto the surface of the local unit cube
		position.Y = 1;

		// Apply mapping formula
		float x2 = position.X * position.X;
		float y2 = position.Y * position.Y;
		float z2 = position.Z * position.Z;

		float x = position.X * Mathf.Sqrt(1 - (y2 / 2) - (z2 / 2) + (y2 * z2 / 3));
		float y = position.Y * Mathf.Sqrt(1 - (z2 / 2) - (x2 / 2) + (z2 * x2 / 3));
		float z = position.Z * Mathf.Sqrt(1 - (x2 / 2) - (y2 / 2) + (x2 * y2 / 3));

		// Scale it up to sphere shell
		Vector3 mapping = new Vector3(x, y, z) * (radius + yLevel);

		// Undo cube offset
		mapping -= Vector3.Up * radius;

		// Return to original basis
		return mapping * basis;
	}
}
