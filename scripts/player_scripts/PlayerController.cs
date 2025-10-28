using Godot;
using System;

/*
This script is based on a modified Unity script for Dani's FPS controller. 
The original script was changed to work on planetary surfaces by being only relative to player rotation.
This script is a ported version of that for 0xDEC.
To account for cases of State Step, I have chosen to multiply by 100 to cancel it and keep forces in sensible ranges.
*/

public partial class PlayerController : RigidBody3D
{
	[Export] int player = 1;
	[Export] public PlayerInput playerInput;
	[Export] public bool useGravity = true;

	public void Set(int id)
	{
		player = id;
		playerInput.SetMultiplayerAuthority(id);
		GD.Print("Attempted set");
	}

	private float camX, camY;
	private float x, y;
	private bool jumping, grounded, jumped;
	private Vector2 mouseDelta = Vector2.Zero;

	// Movement settings
	[Export] public float movementForce = 50;
	[Export] public float jumpForce = 7;
	[Export] public float extraGravity = 50;
	[Export] public float counterMovement = 10;
	[Export] public float maxSpeed = 20;

	// Camera settings
	[Export] public float mouseSensitivity = 10;
	[Export] public Node3D cameraAnchor;
	[Export] public Node3D body;
	[Export] public RayCast3D groundRayCast;

	[Export] public Node3D planetaryObject;


	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		if (!useGravity)
		{
			GravityScale = 0;
		}
	}

	// Player Input
	public override void _Process(double delta)
	{
		//if (GetTree().Paused || GameManager.instance.debug) { return; }

		MyInput();
		Look((float)delta);
		if (Position.Y <= -10)
		{
			//Position = new Vector3(0, 1, 2);
		}
	}

	private void MyInput()
	{
		// x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		// y = -(Input.GetActionStrength("move_forward") - Input.GetActionStrength("move_back"));
		// jumping = Input.IsActionPressed("move_up");

		x = playerInput.inputDirection.X;
		y = playerInput.inputDirection.Y;
		jumping = playerInput.jumping;
	}

	private void Look(float delta)
	{
		camX -= mouseDelta.Y * mouseSensitivity * (float)delta;
		camX = Mathf.Clamp(camX, -90f, 90f);
		camY -= mouseDelta.X * mouseSensitivity * (float)delta;

		cameraAnchor.RotationDegrees = new Vector3(camX, 0, 0);
		body.RotationDegrees = new Vector3(0, camY, 0);

		mouseDelta = Vector2.Zero;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			mouseDelta = mouseMotion.Relative;
		}
	}

	private float lookSpeed = 0.1f; 

	// Phyisics Application
	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		// Rotate to face body
		

		// Custom gravity
		if (!useGravity)
		{
			//ApplyCentralForce(-body.Basis.Y * 9.8f * Mass);
		}

		// Figure out extra gravity later
		ApplyCentralForce(-body.Basis.Y * extraGravity);

		Basis inverseBodyBasis = body.Transform.Basis.Inverse();
		Vector3 localVelocity = inverseBodyBasis * state.LinearVelocity;
		float xMag = localVelocity.X;
		float zMag = localVelocity.Z;

		if (x > 0 && xMag > maxSpeed) x = 0;
		if (x < 0 && xMag < -maxSpeed) x = 0;
		if (y > 0 && zMag > maxSpeed) y = 0;
		if (y < 0 && zMag < -maxSpeed) y = 0;

		Vector3 movementDir = new Vector3(x, 0, y).Normalized();
		Vector3 inputForce = movementDir * movementForce * state.Step * 100;

		if (!grounded)
		{
			inputForce *= 0.5f;
		}

		Vector3 globalForce = body.Transform.Basis * inputForce;
		ApplyCentralForce(globalForce);

		CounterMovement(state);
	}

	private void CounterMovement(PhysicsDirectBodyState3D state)
	{
		if (!grounded || jumping)
		{
			return;
		}

		Vector2 velRelative = FindRelativeToLook(state.LinearVelocity);

		float xMag = velRelative.X;
		float yMag = velRelative.Y;
		

		if ((Mathf.Abs(xMag) > 0.01f && Mathf.Abs(x) < 0.05f) || (xMag < -0.01f && x > 0) || (xMag > 0.01f && x < 0))
		{
			ApplyCentralForce(-body.Basis.X * xMag * counterMovement * state.Step * 100);
		}

		if ((Mathf.Abs(yMag) > 0.01f && Mathf.Abs(y) < 0.05f) || (yMag < -0.01f && y > 0) || (yMag > 0.01f && y < 0))
		{
			ApplyCentralForce(-body.Basis.Z * yMag * counterMovement * state.Step * 100);
		}
	}

	private Vector2 FindRelativeToLook(Vector3 velocity)
	{
		Vector3 localRight = body.Basis.X;
		Vector3 localForward = body.Basis.Z;

		float rightVel = velocity.Dot(localRight);
		float forwardVel = velocity.Dot(localForward);

		return new Vector2(rightVel, forwardVel);
	}

	public override void _PhysicsProcess(double delta)
	{
		grounded = groundRayCast.IsColliding();

		if (jumping && grounded && !jumped)
		{
			Jump();
		}
	}

	private void Jump()
	{
		Vector3 localUp = body.Basis.Y;
		ApplyCentralImpulse(localUp * jumpForce);

		ResetJumpAsync();
	}

	private async void ResetJumpAsync()
	{
		jumped = true;
		await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		jumped = false;
	}
}
