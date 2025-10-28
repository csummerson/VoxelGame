using Godot;
using System;
using System.Linq;

public partial class Main : Node
{
	[Export] public PackedScene serverScene;
	[Export] public PackedScene[] playerScenes;

	public override void _Ready()
	{
		if (OS.HasFeature("dedicated_server"))
		{
			StartServer();
		}
		else
		{
			StartPlayer();
		}
	}

	private void StartServer()
	{
		GD.Print("Launching as server...");
		NetworkManager.instance.isServer = true;
		NetworkManager.instance.StartServer();
		GetTree().CallDeferred("change_scene_to_packed", serverScene);
	}

	private void StartPlayer()
	{
		GD.Print("Launching as player...");
		int saveState = SaveManager.Instance.LoadData();



		GetTree().CallDeferred("change_scene_to_packed", playerScenes[saveState]);
	}
}
