using Godot;
using System;

public partial class HitscanWeapon : RayCast3D
{
	[Export] public float Damage = 25f;
	[Export] public float FireRate = 0.15f;

	[Export] public float RecoilUp = 0.03f;
	[Export] public float RecoilSide = 0.01f;
	[Export] public float RecoilRecoverySpeed = 14f;

	[Export] public float BaseSpread = 0.008f;
	[Export] public float MoveSpread = 0.04f;
	[Export] public uint CollisionMask = 1;

	public float CurrentSpread { get; private set; } = 0.008f;

	private float _nextFireTime = 0f;
	private Marker3D _muzzle;
	private Player _ownerPlayer;
	private Camera3D _camera;

	private float _targetRecoilPitch = 0f;
	private float _currentRecoilPitch = 0f;

	public override void _Ready()
	{
		_muzzle = GetNodeOrNull<Marker3D>("../Muzzle");
		_ownerPlayer = GetNodeAncestor<Player>(this);
		_camera = _ownerPlayer?.GetNodeOrNull<Camera3D>("Head/Camera3D");

		if (_ownerPlayer != null)
		{
			AddException(_ownerPlayer);
		}
	}

	public override void _Process(double delta)
	{
		if (_ownerPlayer != null && !_ownerPlayer.IsMultiplayerAuthority()) return;

		float dt = (float)delta;

		// Calculate current spread state
		CurrentSpread = CalculateCurrentSpread();

		// Recoil pitch recovery
		_targetRecoilPitch = Mathf.Lerp(_targetRecoilPitch, 0f, dt * RecoilRecoverySpeed);
		
		float newPitch = Mathf.Lerp(_currentRecoilPitch, _targetRecoilPitch, dt * 25f);
		float pitchDelta = newPitch - _currentRecoilPitch;
		_currentRecoilPitch = newPitch;

		if (_camera != null && !Mathf.IsZeroApprox(pitchDelta))
		{
			_camera.RotateObjectLocal(Vector3.Right, pitchDelta);
		}

		if (Input.IsActionPressed("fire"))
		{
			TryShoot();
		}
	}

	private float CalculateCurrentSpread()
	{
		if (_ownerPlayer == null) return BaseSpread;

		// Air check always adds penalty
		if (!_ownerPlayer.IsOnFloor()) 
		{
			return BaseSpread + MoveSpread * 1.5f;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		float speed = new Vector3(_ownerPlayer.Velocity.X, 0, _ownerPlayer.Velocity.Z).Length();

		// Counter-strafe check: If input is neutral/canceling or speed drops below threshold (1.0 m/s), collapse to BaseSpread
		if (inputDir == Vector2.Zero || speed < 1.0f)
		{
			return BaseSpread;
		}

		// Active movement spread
		return BaseSpread + (speed / 7.0f) * MoveSpread;
	}

	private void TryShoot()
	{
		float currentTime = Time.GetTicksMsec() / 1000f;
		if (currentTime < _nextFireTime) return;

		_nextFireTime = currentTime + FireRate;

		if (_camera == null) return;

		float randX = (float)GD.RandRange(-CurrentSpread, CurrentSpread);
		float randY = (float)GD.RandRange(-CurrentSpread, CurrentSpread);

		Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2.0f;
		Vector3 rayOrigin = _camera.ProjectRayOrigin(screenCenter);
		Vector3 baseDirection = _camera.ProjectRayNormal(screenCenter);

		Vector3 right = _camera.GlobalTransform.Basis.X.Normalized();
		Vector3 up = _camera.GlobalTransform.Basis.Y.Normalized();
		Vector3 fireDirection = (baseDirection + (right * randX) + (up * randY)).Normalized();

		Vector3 targetPoint;
		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
		
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
			rayOrigin, 
			rayOrigin + fireDirection * 500f
		);
		
		query.CollisionMask = CollisionMask;
		query.Exclude = new Godot.Collections.Array<Rid> { _ownerPlayer.GetRid() };
		query.CollideWithBodies = true;
		query.CollideWithAreas = true;

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			targetPoint = (Vector3)result["position"];
			GodotObject collider = (GodotObject)result["collider"];

			if (collider is Node hitNode)
			{
				HealthComponent health = FindHealthComponent(hitNode);
				if (health != null)
				{
					string attackerName = $"Player {_ownerPlayer.Name}";
					health.Rpc(nameof(HealthComponent.TakeDamage), Damage, attackerName);
				}
			}
		}
		else
		{
			targetPoint = rayOrigin + fireDirection * 500f;
		}

		_targetRecoilPitch += RecoilUp;
		_ownerPlayer.RotateY((float)GD.RandRange(-RecoilSide, RecoilSide));

		Vector3 muzzlePos = _muzzle != null ? _muzzle.GlobalPosition : GlobalPosition;
		Rpc(nameof(ShowTracer), muzzlePos, targetPoint);
	}

	private HealthComponent FindHealthComponent(Node startNode)
	{
		Node current = startNode;
		while (current != null)
		{
			HealthComponent health = current.GetNodeOrNull<HealthComponent>("HealthComponent");
			if (health != null) return health;
			current = current.GetParent();
		}
		return null;
	}

	private T GetNodeAncestor<T>(Node startNode) where T : Node
	{
		Node current = startNode.GetParent();
		while (current != null)
		{
			if (current is T ancestor) return ancestor;
			current = current.GetParent();
		}
		return null;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ShowTracer(Vector3 start, Vector3 end)
	{
		float distance = start.DistanceTo(end);
		if (distance < 0.1f) return;

		MeshInstance3D beam = new MeshInstance3D();
		CylinderMesh cylinder = new CylinderMesh
		{
			TopRadius = 0.015f,
			BottomRadius = 0.015f,
			Height = distance
		};
		beam.Mesh = cylinder;

		StandardMaterial3D mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(1f, 0.85f, 0.3f),
			EmissionEnabled = true,
			Emission = new Color(1f, 0.85f, 0.3f),
			EmissionEnergyMultiplier = 6.0f
		};
		beam.MaterialOverride = mat;

		GetTree().Root.AddChild(beam);

		beam.GlobalPosition = start.Lerp(end, 0.5f);
		
		if (start.DistanceSquaredTo(end) > 0.001f)
		{
			beam.LookAt(end, Vector3.Up);
			beam.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(90));
		}

		GetTree().CreateTimer(0.04f).Timeout += () => beam.QueueFree();
	}
}
