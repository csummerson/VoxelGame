using Godot;
using System;

public partial class PlayerManager : Node3D
{
	[Export] Camera3D camera;

	Color[] colors = new Color[] {
		new Color(1, 0, 0, 1),      // red
		new Color(0, 1, 0, 1),      // green
		new Color(0, 0, 1, 1),      // blue
		new Color(1, 1, 0, 1),      // yellow
		new Color(1, 0, 1, 1),      // magenta
		new Color(0, 1, 1, 1),      // cyan
		new Color(1, 0.5f, 0, 1),   // orange
		new Color(0.5f, 0, 1, 1),   // purple
		new Color(0.5f, 0.5f, 0.5f, 1), // gray
	};

	public override void _EnterTree()
	{
		if (false)
		{
			SetMultiplayerAuthority(int.Parse(Name.ToString()));
			camera.Current = IsMultiplayerAuthority();
		}
		
		int id = GetMultiplayerAuthority();

		MeshInstance3D mesh = GetNode<MeshInstance3D>("Player/Mesh");
		StandardMaterial3D material = mesh.MaterialOverride as StandardMaterial3D;
		if (material == null)
		{
			material = new StandardMaterial3D();
			mesh.MaterialOverride = material;
		}
		material.AlbedoColor = colors[id % colors.Length];

		Label3D label = GetNode<Label3D>("Player/Nickname");
		label.Text = GameManager.Instance.username;
	}
}
