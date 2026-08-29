using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// csgo kinda params
	[Export] public float MaxSpeed = 7.0f;
	[Export] public float CrouchSpeed = 3.5f;
	[Export] public float Accel = 5.5f;
	[Export] public float AirAccel = 12.0f;
	[Export] public float AirCap = 0.8f;
	[Export] public float Gravity = 18.0f;
	[Export] public float JumpForce = 6.5f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float Friction = 5.2f;

	[Export] public float StandHeight = 2.0f;
	[Export] public float CrouchHeight = 1.0f;
	[Export] public float CrouchSpeedTransition = 12.0f;

	[Export] public Color PlayerColor { get; set; } = Colors.White;

	private Node3D _head;
	private Camera3D _camera;
	private CollisionShape3D _collider;
	private MeshInstance3D _mesh;
	
	private float _cameraRotationX = 0f;
	private float _currentShapeHeight;
	private bool _isCrouching = false;

	public override void _EnterTree()
	{
		// netowrk authority based on node spawn
		if (int.TryParse(Name, out int peerId))
		{
			SetMultiplayerAuthority(peerId);
		}
	}

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_collider = GetNode<CollisionShape3D>("CollisionShape3D");
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		_currentShapeHeight = StandHeight;
		
		ApplyPlayerColor();
		
		// only local
		if (IsMultiplayerAuthority())
		{
			_camera.MakeCurrent();
			_mesh.Hide(); // Hide local capsule mesh
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			_mesh.Show(); 
		}
	}

	public void SetRandomColor()
	{
		PlayerColor = Color.FromHsv((float)GD.RandRange(0.0, 1.0), 0.8f, 0.9f);
		ApplyPlayerColor();
	}

	private void ApplyPlayerColor()
	{
		if (_mesh == null) return;

		StandardMaterial3D mat = new StandardMaterial3D();
		mat.AlbedoColor = PlayerColor;
		_mesh.MaterialOverride = mat;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// only local
		if (!IsMultiplayerAuthority()) return;

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			_cameraRotationX -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraRotationX = Mathf.Clamp(_cameraRotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
			_head.Rotation = new Vector3(_cameraRotationX, _head.Rotation.Y, _head.Rotation.Z);
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// only local
		if (!IsMultiplayerAuthority()) return;

		float dt = (float)delta;
		Vector3 vel = Velocity;

		_isCrouching = Input.IsActionPressed("crouch");
		UpdateCrouchState(dt);

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 wishDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		float targetMaxSpeed = _isCrouching ? CrouchSpeed : MaxSpeed;

		if (IsOnFloor())
		{
			vel = ApplyFriction(vel, dt);
			vel = Accelerate(vel, wishDir, targetMaxSpeed, Accel, dt);

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

	private void UpdateCrouchState(float dt)
	{
		float targetHeight = _isCrouching ? CrouchHeight : StandHeight;
		_currentShapeHeight = Mathf.Lerp(_currentShapeHeight, targetHeight, dt * CrouchSpeedTransition);

		if (_collider.Shape is CapsuleShape3D capsule)
		{
			capsule.Height = _currentShapeHeight;
		}

		Vector3 headPos = _head.Position;
		headPos.Y = _currentShapeHeight * 0.75f - 0.5f;
		_head.Position = headPos;
	}

	private Vector3 ApplyFriction(Vector3 currentVel, float dt)
	{
		Vector3 horizontalVel = new Vector3(currentVel.X, 0, currentVel.Z);
		float speed = horizontalVel.Length();

		if (speed < 0.1f) return new Vector3(0, currentVel.Y, 0);

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
