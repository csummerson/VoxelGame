using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public int MODEL = (int) WorldModel.Smooth;
	public int RD = 8;
	public int SD = 8;
	public int SIZE = 8;
	public bool hasSeenTerminal = false;

	public string chunkInfo = "";
	public string worldSeed = "";

	public enum WorldModel
	{
		Flat,
		Smooth
	}

	public override void _EnterTree()
	{
		if (Instance == null)
		{
			Instance = this;
			SetProcess(false);
		}
		else
		{
			QueueFree();
		}
	}

	public void StartSinglePlayer()
	{
		LevelManager levelManager = GetTree().CurrentScene as LevelManager;
		GD.Print("Running?");
		if (levelManager != null)
		{
			levelManager.AddSinglePlayer();
		}
	}

    public override void _Notification(int what)
    {
		if (what == NotificationWMCloseRequest)
		{
			GD.Print("Game is quitting.");
			SaveManager.Instance.SaveData();
		}
    }


	public string username;
}
