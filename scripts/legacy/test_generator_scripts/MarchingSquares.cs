using Godot;
using System;
using System.Collections.Generic;

public partial class MarchingSquares : Node2D
{
	Color dotFilled = new Color(1, 1, 1);
	Color dotEmpty = new Color(0, 0, 0);
	Color lineColor = new Color(1, 0, 0);

	[Export] int sizeX = 16, sizeY = 16;
	[Export] Vector2 gridOffset = new Vector2(75, 75);
	[Export] float gridScale = 25;

	FastNoiseLite noise = new FastNoiseLite();

	List<List<int>> matrix = new List<List<int>>();

	public Dictionary<int, string[]> configurations;

	public override void _Ready()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		noise.Seed = rng.RandiRange(0, 1000);
		noise.Frequency = 0.1f;
		noise.FractalOctaves = 4;
		noise.FractalLacunarity = 2f;
		noise.FractalGain = 0.5f;

		configurations = new Dictionary<int, string[]>()
		{
			{0, new string[] { } },
			{ Bin2Int("0001"), new string[] { "e", "h" } },
			{ Bin2Int("0010"), new string[] { "e", "f" } },
			{ Bin2Int("0100"), new string[] { "f", "g" } },
			{ Bin2Int("1000"), new string[] { "g", "h" } },
			{ Bin2Int("0011"), new string[] { "h", "f" } },
			{ Bin2Int("0110"), new string[] { "e", "g" } },
			{ Bin2Int("1100"), new string[] { "h", "f" } },
			{ Bin2Int("1001"), new string[] { "e", "g" } },
			{ Bin2Int("0101"), new string[] { "h", "e", "g", "f" } },
			{ Bin2Int("1010"), new string[] { "h", "g", "e", "f" } },
			{ Bin2Int("0111"), new string[] { "h", "g" } },
			{ Bin2Int("1110"), new string[] { "h", "e" } },
			{ Bin2Int("1101"), new string[] { "e", "f" } },
			{ Bin2Int("1011"), new string[] { "g", "f" } },
		};

		GenerateSquares();
	}

	public void GenerateSquares()
	{
		for (int i = 0; i < sizeY; i++)
		{
			matrix.Add(new List<int>());

			for (int j = 0; j < sizeX; j++)
			{
				float noiseValue = noise.GetNoise2D(i, j);
				int val = noiseValue > 0 ? 1 : 0;
				matrix[i].Add(val);
			}
		}
	}

	public override void _Draw()
	{
		for (int i = 0; i < sizeY; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				if (matrix[i][j] == 0)
				{
					DrawCircle(new Vector2(i, j) * gridScale + gridOffset, 2.5f, dotEmpty);
				}
				else
				{
					DrawCircle(new Vector2(i, j) * gridScale + gridOffset, 2.5f, dotFilled);
				}
			}
		}

		MarchThemSquares();
	}

	private void MarchThemSquares()
	{
		for (int i = 0; i < sizeY - 1; i++)
		{
			for (int j = 0; j < sizeX - 1; j++)
			{
				int a = matrix[i][j];
				int b = matrix[i + 1][j];
				int c = matrix[i + 1][j + 1];
				int d = matrix[i][j + 1];

				if (a == b && b == c && c == d)
				{
					continue;
				}

				Vector2 aPos = new Vector2(i, j) * gridScale + gridOffset;
				Vector2 bPos = new Vector2(i + 1, j) * gridScale + gridOffset;
				Vector2 cPos = new Vector2(i + 1, j + 1) * gridScale + gridOffset;
				Vector2 dPos = new Vector2(i, j + 1) * gridScale + gridOffset;

				Vector2 e = (bPos - aPos) / 2 + aPos;
				Vector2 f = (cPos - bPos) / 2 + bPos;
				Vector2 g = (dPos - cPos) / 2 + cPos;
				Vector2 h = (dPos - aPos) / 2 + aPos;

				Dictionary<string, Vector2> dicPoints = new Dictionary<string, Vector2>()
				{
					{ "e", e },
					{ "f", f },
					{ "g", g },
					{ "h", h }
				};

				int configuration = a * 1 + b * 2 + c * 4 + d * 8;
				string[] pointsToConnect = configurations[configuration];

				for (int k = 0; k < pointsToConnect.Length; k += 2)
                {
                    Vector2 pointAPos = dicPoints[pointsToConnect[k]];
                    Vector2 pointBPos = dicPoints[pointsToConnect[k + 1]];
                    DrawLine(pointAPos, pointBPos, lineColor, 2);
                }
			}
		}
	}

	private int Bin2Int(string binStr)
    {
        int result = 0;
        foreach (char c in binStr)
        {
            result = (result << 1) + (c == '1' ? 1 : 0);
        }
        return result;
    }
}
