using Godot;
using System;

public partial class Player : RigidBody3D
{
    [Export] Node3D planetaryBody;
    float rotationSpeed = 0.01f;

    [Export] public PlayerInput playerInput;

    private float camX, camY;
	private float x, y;
	private bool jumping, grounded, jumped;
	private Vector2 mouseDelta = Vector2.Zero;

	// Movement settings
	[Export] public float movementForce = 15;
	[Export] public float jumpForce = 6;
	[Export] public float extraGravity = 2;
	[Export] public float counterMovement = 10;
	[Export] public float maxSpeed = 10;

	// Camera settings
	[Export] public float mouseSensitivity = 10;
	[Export] public Node3D cameraAnchor;
	[Export] public RayCast3D groundRayCast;

	public override void _Ready()
	{
		Position = Vector3.Up * (GameManager.Instance.SIZE + 3) * 16;

		Input.MouseMode = Input.MouseModeEnum.Captured;

		if (planetaryBody != null)
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

        // FIX
		//RotationDegrees = new Vector3(0, camY, 0);

		mouseDelta = Vector2.Zero;
	}

	public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            mouseDelta = mouseMotion.Relative;
        }
    }

    // Physics Applications
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        

        if (planetaryBody != null)
        {
            // Peform orbital rotation operations
            Vector3 targetPosition = planetaryBody.GlobalTransform.Origin;
            LookFollow(state, GlobalTransform, targetPosition);

            Vector3 planetaryUp = (GlobalTransform.Origin - targetPosition).Normalized();
            float yawSpeed = -mouseDelta.X * mouseSensitivity * (float)state.Step;

            state.AngularVelocity += planetaryUp * yawSpeed;

            // Use custom planetary gravity
            ApplyCentralForce((planetaryBody.GlobalPosition - GlobalPosition).Normalized() * 9.8f * Mass);
        } else
        {
			float yawSpeed = -mouseDelta.X * mouseSensitivity * (float)state.Step;
			state.AngularVelocity += Vector3.Up * yawSpeed;
        }

        // Normal Calculations
        Basis inverseBasis = Transform.Basis.Inverse();
        Vector3 localVelocity = inverseBasis * state.LinearVelocity;
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

        Vector3 globalForce = Transform.Basis * inputForce;
        ApplyCentralForce(globalForce * Mass);

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
			ApplyCentralForce(-Basis.X * xMag * counterMovement * state.Step * 100 * Mass);
		}

		if ((Mathf.Abs(yMag) > 0.01f && Mathf.Abs(y) < 0.05f) || (yMag < -0.01f && y > 0) || (yMag > 0.01f && y < 0))
		{
			ApplyCentralForce(-Basis.Z * yMag * counterMovement * state.Step * 100 * Mass);
		}
	}

    private Vector2 FindRelativeToLook(Vector3 velocity)
	{
		Vector3 localRight = Basis.X;
		Vector3 localForward = Basis.Z;

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
		Vector3 localUp = Basis.Y;
		ApplyCentralImpulse(localUp * jumpForce * Mass);

		ResetJumpAsync();
	}

	private async void ResetJumpAsync()
	{
		jumped = true;
		await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		jumped = false;
	}


    private void LookFollow(PhysicsDirectBodyState3D state, Transform3D currentTransform, Vector3 targetPosition)
    {
        // final up relative to the planet
        Vector3 planetaryUp = (currentTransform.Origin - targetPosition).Normalized();

        // create forward axis based on that
        Vector3 forwardDir = (currentTransform.Basis * Vector3.Up).Normalized();
        Vector3 targetDir = planetaryUp;

        float dot = Mathf.Clamp(forwardDir.Dot(targetDir), -1.0f, 1.0f);
        float angle = Mathf.Acos(dot);

        float localSpeed = Mathf.Clamp(rotationSpeed, 0.0f, angle);

        // rotate to orientation
        if (angle > 1e-4f)
        {
            Vector3 axis = forwardDir.Cross(targetDir);

            if (axis.LengthSquared() < 1e-6f)
            {
                axis = planetaryUp.Cross(forwardDir).Normalized();
            }
            else
            {
                axis = axis.Normalized();
            }

            state.AngularVelocity = axis * localSpeed / state.Step;

            //GD.Print("Rotating");
        }
        else
        {
            state.AngularVelocity = Vector3.Zero; // stop rotating to prevent overcompensating

            //GD.Print("Stopped");
        }
    }
}
