using Godot;
using System;

public partial class HitscanWeapon : RayCast3D
{
	[Export] public float Damage = 25f;
	[Export] public float FireRate = 0.15f;

	[Export] public float RecoilUp = 0.04f;      // Vertical camera kick per shot (radians)
	[Export] public float RecoilSide = 0.015f;   // Random horizontal camera jitter (radians)
	[Export] public float RecoilRecoverySpeed = 10f;

	[Export] public float BaseSpread = 0.015f;   // Base accuracy when standing still
	[Export] public float MoveSpread = 0.08f;    // Max spread added when sprinting/jumping

	private float _nextFireTime = 0f;
	private Marker3D _muzzle;
	private Player _ownerPlayer;
	private Node3D _head;

	private float _targetRecoilPitch = 0f;
	private float _targetRecoilYaw = 0f;
	private float _currentRecoilPitch = 0f;
	private float _currentRecoilYaw = 0f;

	public override void _Ready()
	{
		_muzzle = GetNodeOrNull<Marker3D>("../Muzzle");
		_ownerPlayer = GetNodeAncestor<Player>(this);
		_head = GetNodeOrNull<Node3D>("../"); // Parent Head node

		if (_ownerPlayer != null)
		{
			AddException(_ownerPlayer);
		}
	}

	public override void _Process(double delta)
	{
		if (_ownerPlayer != null && !_ownerPlayer.IsMultiplayerAuthority()) return;

		float dt = (float)delta;

		// return camera recoil back to center
		_targetRecoilPitch = Mathf.Lerp(_targetRecoilPitch, 0f, dt * RecoilRecoverySpeed);
		_targetRecoilYaw = Mathf.Lerp(_targetRecoilYaw, 0f, dt * RecoilRecoverySpeed);

		float pitchDelta = Mathf.Lerp(_currentRecoilPitch, _targetRecoilPitch, dt * 25f) - _currentRecoilPitch;
		float yawDelta = Mathf.Lerp(_currentRecoilYaw, _targetRecoilYaw, dt * 25f) - _currentRecoilYaw;

		_currentRecoilPitch += pitchDelta;
		_currentRecoilYaw += yawDelta;

		if (_head != null)
		{
			_head.RotateX(pitchDelta);
			_ownerPlayer.RotateY(yawDelta);
		}

		if (Input.IsActionPressed("fire"))
		{
			TryShoot();
		}
	}

	private void TryShoot()
	{
		float currentTime = Time.GetTicksMsec() / 1000f;
		if (currentTime < _nextFireTime) return;

		_nextFireTime = currentTime + FireRate;

		// 1. Calculate dynamic bullet spread based on horizontal velocity and air state
		float speed = _ownerPlayer != null ? new Vector3(_ownerPlayer.Velocity.X, 0, _ownerPlayer.Velocity.Z).Length() : 0f;
		float currentSpread = BaseSpread + (speed / 7.0f) * MoveSpread;
		if (_ownerPlayer != null && !_ownerPlayer.IsOnFloor()) currentSpread += MoveSpread * 1.5f;

		// Random directional offset within spread cone
		Vector3 spreadOffset = new Vector3(
			(float)GD.RandRange(-currentSpread, currentSpread),
			(float)GD.RandRange(-currentSpread, currentSpread),
			0f
		);

		Vector3 fireDirection = (-GlobalTransform.Basis.Z + GlobalTransform.Basis * spreadOffset).Normalized();

		// Perform raycast check using spread direction
		Vector3 targetPoint;
		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(GlobalPosition, GlobalPosition + fireDirection * 100f);
		query.Exclude = new Godot.Collections.Array<Rid> { _ownerPlayer.GetRid() };

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
					string attackerName = _ownerPlayer != null ? $"Player {_ownerPlayer.Name}" : "Player";
					health.Rpc(nameof(HealthComponent.TakeDamage), Damage, attackerName);
				}
			}
		}
		else
		{
			targetPoint = GlobalPosition + fireDirection * 100f;
		}

		_targetRecoilPitch += RecoilUp;
		_targetRecoilYaw += (float)GD.RandRange(-RecoilSide, RecoilSide);

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
			TopRadius = 0.05f,
			BottomRadius = 0.05f,
			Height = distance
		};
		beam.Mesh = cylinder;

		StandardMaterial3D mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(1f, 0.5f, 0.1f),
			EmissionEnabled = true,
			Emission = new Color(1f, 0.6f, 0.1f),
			EmissionEnergyMultiplier = 16.0f
		};
		beam.MaterialOverride = mat;

		GetTree().Root.AddChild(beam);
		beam.GlobalPosition = start.Lerp(end, 0.5f);
		
		if (start.DistanceSquaredTo(end) > 0.001f)
		{
			beam.LookAt(end, Vector3.Up);
			beam.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(90));
		}

		GetTree().CreateTimer(0.07f).Timeout += () => beam.QueueFree();
	}
}
