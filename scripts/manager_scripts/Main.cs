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
		SaveManager.Instance.LoadData();
		GameSettings.Instance.ManualLoad();

		GD.Print("Launching as client...");
		
		if (GameSettings.Instance.hasSeenTerminal)
		{
			GetTree().CallDeferred("change_scene_to_packed", playerScenes[1]);
		} else
        {
            GetTree().CallDeferred("change_scene_to_packed", playerScenes[0]);
        }
	}
}
