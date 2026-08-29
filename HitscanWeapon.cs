using Godot;
using System;

public partial class HitscanWeapon : RayCast3D
{
	[Export] public float Damage = 25f;
	[Export] public float FireRate = 0.15f;

	private float _nextFireTime = 0f;
	private Marker3D _muzzle;

	public override void _Ready()
	{
		// Get reference to the Muzzle marker node
		_muzzle = GetNodeOrNull<Marker3D>("../Muzzle");

		Player playerRoot = GetNodeAncestor<Player>(this);
		if (playerRoot != null)
		{
			AddException(playerRoot);
		}
	}

	public override void _Process(double delta)
	{
		Player playerRoot = GetNodeAncestor<Player>(this);
		if (playerRoot != null && !playerRoot.IsMultiplayerAuthority()) return;

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

		Vector3 targetPoint;

		if (IsColliding())
		{
			targetPoint = GetCollisionPoint();

			GodotObject collider = GetCollider();
			
			if (collider is Node hitNode)
			{
				HealthComponent health = FindHealthComponent(hitNode);
				if (health != null)
				{
					Player ownerPlayer = GetNodeAncestor<Player>(this);
					string attackerName = ownerPlayer != null ? $"Player {ownerPlayer.Name}" : "Player";

					health.Rpc(nameof(HealthComponent.TakeDamage), Damage, attackerName);
				}
			}
		}
		else
		{
			targetPoint = GlobalPosition + (-GlobalTransform.Basis.Z * 100f);
		}

		//u se Muzzle position as visual start point, falling back to camera position if missing
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
