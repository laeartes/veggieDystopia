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
	[Export] public float StopSpeed = 1.5f; // Velocity floor for linear friction drop

	[Export] public float StandHeight = 2.0f;
	[Export] public float CrouchHeight = 1.0f;
	[Export] public float CrouchSpeedTransition = 12.0f;

	[Export] public Color PlayerColor { get; set; } = Colors.White;

	private Node3D _head;
	private Camera3D _camera;
	private CollisionShape3D _collider;
	private CapsuleShape3D _capsuleShape;
	private MeshInstance3D _mesh;
	private RayCast3D _headCheck;
	private Label _velocityLabel;
	private float _cameraRotationX = 0f;
	private float _currentShapeHeight;
	private bool _isCrouching = false;

	// Jump buffering for scrollwheel + spacebar responsiveness
	private float _jumpBufferTimer = 0f;
	private const float JUMP_BUFFER_TIME = 0.1f;

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
		_headCheck = GetNodeOrNull<RayCast3D>("HeadCheck");
		_currentShapeHeight = StandHeight;
		_velocityLabel = GetNodeOrNull<Label>("HUD/Velocity");
		// Duplicate shape resource to prevent shared height mutation across instances
		_capsuleShape = (CapsuleShape3D)_collider.Shape.Duplicate();
		_collider.Shape = _capsuleShape;
		
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

		// Buffer jump input on wheel scroll or keypress
		if (@event.IsActionPressed("jump"))
		{
			_jumpBufferTimer = JUMP_BUFFER_TIME;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// only local
		if (!IsMultiplayerAuthority()) return;

		float dt = (float)delta;
		Vector3 vel = Velocity;

		// Tick down jump buffer
		if (_jumpBufferTimer > 0f)
		{
			_jumpBufferTimer -= dt;
		}

		bool wantCrouch = Input.IsActionPressed("crouch");
		// Prevent uncrouching under low ceilings
		if (!wantCrouch && _isCrouching && _headCheck != null && _headCheck.IsColliding())
		{
			wantCrouch = true;
		}
		_isCrouching = wantCrouch;

		UpdateCrouchState(dt);

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 wishDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		float targetMaxSpeed = _isCrouching ? CrouchSpeed : MaxSpeed;

		if (IsOnFloor())
		{
			vel = ApplyFriction(vel, dt);
			vel = Accelerate(vel, wishDir, targetMaxSpeed, Accel, dt);

			if (_jumpBufferTimer > 0f) 
			{
				vel.Y = JumpForce;
				_jumpBufferTimer = 0f;
			}
		}
		else
		{
			vel.Y -= Gravity * dt;
			vel = AirAccelerate(vel, wishDir, targetMaxSpeed, AirCap, AirAccel, dt);
		}

		Velocity = vel;
		MoveAndSlide();
		if (_velocityLabel != null)
		{
			float horizontalSpeed = new Vector3(Velocity.X, 0, Velocity.Z).Length();
			_velocityLabel.Text = $"Speed: {horizontalSpeed:F2} u/s";
		}
	}

	private void UpdateCrouchState(float dt)
	{
		float targetHeight = _isCrouching ? CrouchHeight : StandHeight;
		_currentShapeHeight = Mathf.Lerp(_currentShapeHeight, targetHeight, dt * CrouchSpeedTransition);

		_capsuleShape.Height = _currentShapeHeight;

		Vector3 headPos = _head.Position;
		headPos.Y = _currentShapeHeight * 0.75f - 0.5f;
		_head.Position = headPos;
	}

	private Vector3 ApplyFriction(Vector3 currentVel, float dt)
	{
		Vector3 horizontalVel = new Vector3(currentVel.X, 0, currentVel.Z);
		float speed = horizontalVel.Length();

		if (speed < 0.001f) return new Vector3(0, currentVel.Y, 0);

		// Source engine uses a minimum speed threshold (StopSpeed) for friction calculations
		float control = speed < StopSpeed ? StopSpeed : speed;
		float drop = control * Friction * dt;

		float newSpeed = Mathf.Max(speed - drop, 0f);
		newSpeed /= speed;

		return new Vector3(horizontalVel.X * newSpeed, currentVel.Y, horizontalVel.Z * newSpeed);
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

	private Vector3 AirAccelerate(Vector3 currentVel, Vector3 wishDir, float wishSpeed, float airCap, float accel, float dt)
	{
		// Air cap restricts max speed gain along wishDir, allowing side-strafing speed buildup
		float capSpeed = Mathf.Min(wishSpeed, airCap);
		float currentSpeed = currentVel.Dot(wishDir);
		float addSpeed = capSpeed - currentSpeed;
		if (addSpeed <= 0) return currentVel;

		float accelSpeed = accel * wishSpeed * dt;
		accelSpeed = Mathf.Min(accelSpeed, addSpeed);
		return currentVel + wishDir * accelSpeed;
	}
}
