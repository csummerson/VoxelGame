using Godot;
using System;

public partial class DebugCamera : Node3D
{
	[Export]
	public float MoveSpeed = 3f;

	[Export]
	public float MouseSensitivity = 1f;

	[Export]
	Camera3D Camera;

	float camX, camY;
	Vector2 mouseDelta = Vector2.Zero;

	bool active = true;

	[Export] bool placeOnSurface = false;
	[Export] int chunkRadius = 4;

	[Export] bool isactive = false;

	public override void _Ready()
	{
		if (placeOnSurface) Position = new Vector3(0, (GameManager.Instance.SIZE + 3) * 16, 0);

		if (!isactive)
		{
			return;
		}
		
		Camera.MakeCurrent();
	}

	public override void _Process(double delta)
	{
		Move(delta);
		Look(delta);
	}

	public void Move(double delta)
	{
		if (Input.IsActionPressed("move_right"))
		{
			Translate(Vector3.Right * MoveSpeed * (float)delta);
		}

		if (Input.IsActionPressed("move_left"))
		{
			Translate(Vector3.Left * MoveSpeed * (float)delta);
		}

		if (Input.IsActionPressed("move_forward"))
		{
			Translate(Vector3.Forward * MoveSpeed * (float)delta);
		}

		if (Input.IsActionPressed("move_back"))
		{
			Translate(Vector3.Back * MoveSpeed * (float)delta);
		}

		if (Input.IsActionPressed("move_up"))
		{
			Translate(Vector3.Up * MoveSpeed * (float)delta);
		}

		if (Input.IsActionPressed("move_down"))
		{
			Translate(Vector3.Down * MoveSpeed * (float)delta);
		}
	}

	public void Look(double delta)
	{
		MouseSensitivity = GameSettings.Instance.mouseSensitivity / 200f;
		
		camX -= mouseDelta.Y * MouseSensitivity;
		camX = Mathf.Clamp(camX, -90f, 90f);
		camY = -mouseDelta.X * MouseSensitivity;

		Camera.RotationDegrees = new Vector3(camX, 0, 0);
		RotateObjectLocal(Vector3.Up, Mathf.DegToRad(camY));

		mouseDelta = Vector2.Zero;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			mouseDelta = mouseMotion.Relative;
		}
	}
}
