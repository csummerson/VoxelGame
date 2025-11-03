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

	public bool canPause = true;

	public enum WorldModel
	{
		Flat,
		Smooth
	}

	public override void _EnterTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

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

    public override void _Ready()
    {
		ProcessMode = ProcessModeEnum.Always;
    }

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (canPause) TogglePause();
		}

		if (@event.IsActionPressed("debug_draw"))
		{
			ToggleDebugDraw();
		}
	}

	private bool isPaused = false;
	private void TogglePause()
	{
		isPaused = !isPaused;
		GetTree().Paused = isPaused;

		if (!isPaused)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	private bool isDebugDrawing = false;
	private void ToggleDebugDraw()
	{
		isDebugDrawing = !isDebugDrawing;

		if (!isDebugDrawing)
		{
			GetViewport().DebugDraw = Viewport.DebugDrawEnum.Disabled;
		}
		else
		{
			GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;
		}
	}

	public void QuitGame()
	{
		GameSettings.Instance.Save();
		SaveManager.Instance.SaveData();
		GetTree().Quit();
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
			GD.Print("Process terminating...");
			SaveManager.Instance.SaveData();
		}
    }


	public string username;
}
