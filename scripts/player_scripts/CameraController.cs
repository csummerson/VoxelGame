using Godot;
using System;

public partial class CameraController : Node3D
{
	[Export] public Node3D anchor;
	[Export] public Camera3D mainCam;

	

	public override void _Process(double delta)
	{
		if (IsMultiplayerAuthority())
            CallDeferred(nameof(UpdateCamera));

	}

	private void UpdateCamera()
	{
		GlobalPosition = anchor.GlobalPosition;
		GlobalRotation = anchor.GlobalRotation;
	}
}
