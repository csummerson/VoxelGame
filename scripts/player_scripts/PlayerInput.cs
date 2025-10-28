using Godot;
using System;

public partial class PlayerInput : MultiplayerSynchronizer
{
	[Export] public bool jumping = false;
	[Export] public Vector2 inputDirection = Vector2.Zero;

	public override void _Process(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
			if (Input.IsActionPressed("move_up"))
			{
				jumping = true;
			}
			else
			{
				jumping = false;
			}
		}
	}
}
