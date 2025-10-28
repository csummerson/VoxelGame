using Godot;
using System;

public partial class LevelManager : Node3D
{
	[Export] PackedScene playerScene;
	[Export] Node3D playerSpawner;
	[Export] PackedScene terminal;

	public override void _Ready()
	{
		if (NetworkManager.instance.isServer)
		{
			Multiplayer.PeerConnected += AddPlayer;
			Multiplayer.PeerDisconnected += DeletePlayer;
			
		}
		else
		{
			Node terminalScene = terminal.Instantiate();
			GetTree().CurrentScene.AddChild(terminalScene);
		}

	}

	public void AddSinglePlayer()
	{
		var character = playerScene.Instantiate();
		character.Name = "1";
		playerSpawner.AddChild(character);

		GD.Print("Started singleplayer.");
	}

	private void AddPlayer(long id)
	{
		var character = playerScene.Instantiate();
		character.Name = id.ToString();
		playerSpawner.AddChild(character);

		GD.Print("Created player with id " + id);
	}

	private void DeletePlayer(long id)
	{
		if (!playerSpawner.HasNode(id.ToString()))
		{
			return;
		}

		playerSpawner.GetNode(id.ToString()).QueueFree();
	}

    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Keycode == Key.Z && GameManager.Instance.hasSeenTerminal == true)
			{
				Node terminalScene = terminal.Instantiate();
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				GetTree().CurrentScene.AddChild(terminalScene);
				NetworkManager.instance.CloseConnection();
			}
		}
    }   
}
