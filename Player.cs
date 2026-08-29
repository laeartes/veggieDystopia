using Godot;
using System;

public partial class Player : CharacterBody3D
{
	//kinda arbitrary values, may need to change
	[Export] public float MaxSpeed = 7.0f;
	[Export] public float Accel = 10.0f;
	[Export] public float AirAccel = 50.0f;
	[Export] public float AirCap = 0.6f;
	[Export] public float Gravity = 20.0f;
	[Export] public float JumpForce = 7.0f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float Friction = 6.0f; 
	
	private Node3D _head;
	private Camera3D _camera;
	private float _cameraRotationX = 0f;
	
	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		Input.MouseMode = Input.MouseModeEnum.Captured; //hide cursor and lock to the center
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			_cameraRotationX -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraRotationX = Mathf.Clamp(_cameraRotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
			_head.Rotation = new Vector3(_cameraRotationX, _head.Rotation.Y, _head.Rotation.Z);
		}

		if (@event.IsActionPressed("ui_cancel")) //escape to free mouse cursor
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _PhysicsProcess(double delta) //decoupled from frame render rate
	{
		float dt = (float)delta;
		Vector3 vel = Velocity;

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 wishDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (IsOnFloor())
		{
			vel = ApplyFriction(vel, dt);
			vel = Accelerate(vel, wishDir, MaxSpeed, Accel, dt);

			if (Input.IsActionJustPressed("jump")) 
			{
				vel.Y = JumpForce;
			}
		}
		else
		{
			vel.Y -= Gravity * dt;
			vel = AirAccelerate(vel, wishDir, AirCap, AirAccel, dt);
		}

		Velocity = vel;
		MoveAndSlide();
	}

	private Vector3 ApplyFriction(Vector3 currentVel, float dt)
	{
		// preservwes vertical vel
		Vector3 horizontalVel = new Vector3(currentVel.X, 0, currentVel.Z);
		float speed = horizontalVel.Length();

		if (speed < 0.1f)
		{
			return new Vector3(0, currentVel.Y, 0);
		}

		float drop = speed * Friction * dt;
		float newSpeed = Mathf.Max(speed - drop, 0f);
		horizontalVel *= (newSpeed / speed);

		return new Vector3(horizontalVel.X, currentVel.Y, horizontalVel.Z);
	}

	private Vector3 Accelerate(Vector3 currentVel, Vector3 wishDir, float maxVelocity, float accel, float dt)
	{
		float currentSpeed = currentVel.Dot(wishDir);
		float addSpeed = maxVelocity - currentSpeed;
		if (addSpeed <= 0) return currentVel;

		float accelSpeed = accel * dt * maxVelocity;
		accelSpeed = Mathf.Min(accelSpeed, addSpeed);
		return currentVel + wishDir * accelSpeed;
	}

	private Vector3 AirAccelerate(Vector3 currentVel, Vector3 wishDir, float wishSpeed, float accel, float dt)
	{
		float currentSpeed = currentVel.Dot(wishDir);
		float addSpeed = wishSpeed - currentSpeed;
		if (addSpeed <= 0) return currentVel;

		float accelSpeed = accel * dt * wishSpeed;
		accelSpeed = Mathf.Min(accelSpeed, addSpeed);
		return currentVel + wishDir * accelSpeed;
	}
}
