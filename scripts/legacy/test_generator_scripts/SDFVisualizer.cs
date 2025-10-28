using Godot;
using System;

public partial class SDFVisualizer : Node2D
{
    [Export] public int Resolution = 256;  // size of the texture
    [Export] public float Threshold = 0.0f; // iso-surface (0 = exact surface)
    [Export] public float Radius = 80.0f;   // circle radius
	[Export] public int gridSize = 16;

    private ImageTexture _texture;
    private Image _image;

    public override void _Ready()
    {
        _image = Image.CreateEmpty(Resolution, Resolution, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
    }

    public override void _Process(double delta)
    {
        DrawSDF();
    }

    private float CircleSDF(Vector2 p, Vector2 center, float radius)
    {
        return p.DistanceTo(center) - radius;
    }

    private void DrawSDF()
    {
        
        Vector2 center = new Vector2(Resolution / 2, Resolution / 2);

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
				if (y % gridSize == 0 && x % gridSize == 0)
				{
					Vector2 p = new Vector2(x, y);
					float d = CircleSDF(p, center, Radius);

					// inside = black, outside = white, near surface = gray
					Color col = d < Threshold ? new Color(1, 1, 1) : new Color(1, 0, 0);

					// optional: visualize gradient
					//if (Math.Abs(d) < 1.0f) col = new Color(1, 0, 0); // red near surface

					_image.SetPixel(x, y, col);
				}
				else
				{
					_image.SetPixel(x, y, new Color(0, 0, 0));
				}
            }
        }

        _texture.Update(_image);
    }

    public override void _Draw()
    {
        if (_texture != null)
        {
            DrawTexture(_texture, Vector2.Zero);
        }
    }
}
