using Godot;
using System;

public partial class HitscanWeapon : RayCast3D
{
	[ExportGroup("Base Weapon Stats")]
	[Export] public float Damage = 25f;
	[Export] public float FireRate = 0.15f;

	[ExportGroup("Recoil Settings")]
	[Export] public float RecoilUp = 0.03f;
	[Export] public float RecoilSide = 0.01f;
	[Export] public float RecoilRecoverySpeed = 14f;

	[ExportGroup("Spread Settings")]
	[Export] public float BaseSpread = 0.008f;
	[Export] public float MoveSpread = 0.04f;
	[Export] public new uint CollisionMask = 1;

	[ExportGroup("Zoom / Scope Settings")]
	[Export] public bool EnableZoom = true;
	[Export] public float ZoomFov = 40.0f;
	[Export] public float ZoomSpeed = 12.0f;
	[Export] public bool HasScopeOverlay = false;
	[Export] public Texture2D ScopeTexture;
	[Export] public float ZoomedSpreadMultiplier = 0.2f;
	[Export] public float ZoomedMoveSpeedMultiplier = 0.6f;

	public float CurrentSpread { get; private set; } = 0.008f;

	private float _nextFireTime = 0f;
	private Marker3D _muzzle;
	private Player _ownerPlayer;
	private Camera3D _camera;

	private float _targetRecoilPitch = 0f;
	private float _currentRecoilPitch = 0f;

	private float _defaultFov = 75.0f;
	private float _targetFov = 75.0f;
	private bool _isZoomed = false;
	private CanvasLayer _scopeCanvas;
	private TextureRect _scopeRect;
	private float _basePlayerMaxSpeed = 7.0f;

	public override void _Ready()
	{
		_ownerPlayer = GetNodeAncestor<Player>(this);
		
		if (IsInstanceValid(_ownerPlayer))
		{
			// Explicitly set multiplayer authority to match the owning player
			SetMultiplayerAuthority(_ownerPlayer.GetMultiplayerAuthority());
			AddException(_ownerPlayer);
			_basePlayerMaxSpeed = _ownerPlayer.MaxSpeed;
		}

		_muzzle = GetNodeOrNull<Marker3D>("Muzzle") ?? GetNodeOrNull<Marker3D>("../Muzzle");
		_camera = _ownerPlayer?.GetNodeOrNull<Camera3D>("Head/Camera3D");

		if (IsInstanceValid(_camera))
		{
			_defaultFov = _camera.Fov;
			_targetFov = _defaultFov;
		}

		SetupScopeOverlay();
	}

	public override void _Process(double delta)
	{
		if (IsInstanceValid(_ownerPlayer) && !_ownerPlayer.IsMultiplayerAuthority()) return;

		float dt = (float)delta;

		if (EnableZoom)
		{
			if (Input.IsActionJustPressed("fire2"))
			{
				SetZoomState(true);
			}
			else if (Input.IsActionJustReleased("fire2"))
			{
				SetZoomState(false);
			}
		}

		if (IsInstanceValid(_camera) && !Mathf.IsEqualApprox(_camera.Fov, _targetFov, 0.05f))
		{
			_camera.Fov = Mathf.Lerp(_camera.Fov, _targetFov, dt * ZoomSpeed);
		}

		CurrentSpread = CalculateCurrentSpread();

		_targetRecoilPitch = Mathf.Lerp(_targetRecoilPitch, 0f, dt * RecoilRecoverySpeed);
		
		float newPitch = Mathf.Lerp(_currentRecoilPitch, _targetRecoilPitch, dt * 25f);
		float pitchDelta = newPitch - _currentRecoilPitch;
		_currentRecoilPitch = newPitch;

		if (IsInstanceValid(_camera) && !Mathf.IsZeroApprox(pitchDelta))
		{
			_camera.RotateObjectLocal(Vector3.Right, pitchDelta);
		}

		if (Input.IsActionPressed("fire"))
		{
			TryShoot();
		}
	}

	public void SetZoomState(bool zoomed)
	{
		_isZoomed = zoomed;
		_targetFov = _isZoomed ? ZoomFov : _defaultFov;

		if (HasScopeOverlay && _scopeCanvas != null)
		{
			_scopeCanvas.Visible = _isZoomed;
		}

		if (IsInstanceValid(_ownerPlayer))
		{
			if (_isZoomed)
			{
				_basePlayerMaxSpeed = _ownerPlayer.MaxSpeed;
				_ownerPlayer.MaxSpeed *= ZoomedMoveSpeedMultiplier;
			}
			else
			{
				_ownerPlayer.MaxSpeed = _basePlayerMaxSpeed;
			}
		}
	}

	protected virtual float CalculateCurrentSpread()
	{
		if (!IsInstanceValid(_ownerPlayer)) return BaseSpread;

		float calculatedSpread;

		if (!_ownerPlayer.IsOnFloor()) 
		{
			calculatedSpread = BaseSpread + MoveSpread * 1.5f;
		}
		else
		{
			Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
			float speed = new Vector3(_ownerPlayer.Velocity.X, 0, _ownerPlayer.Velocity.Z).Length();

			if (inputDir == Vector2.Zero || speed < 1.0f)
			{
				calculatedSpread = BaseSpread;
			}
			else
			{
				calculatedSpread = BaseSpread + (speed / 7.0f) * MoveSpread;
			}
		}

		return _isZoomed ? calculatedSpread * ZoomedSpreadMultiplier : calculatedSpread;
	}

	protected virtual void TryShoot()
	{
		float currentTime = Time.GetTicksMsec() / 1000f;
		if (currentTime < _nextFireTime) return;

		_nextFireTime = currentTime + FireRate;

		if (!IsInstanceValid(_camera)) return;

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
		if (IsInstanceValid(_ownerPlayer))
		{
			query.Exclude = new Godot.Collections.Array<Rid> { _ownerPlayer.GetRid() };
		}
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
		if (IsInstanceValid(_ownerPlayer))
		{
			_ownerPlayer.RotateY((float)GD.RandRange(-RecoilSide, RecoilSide));
		}

		// Determine valid muzzle position or fall back to camera origin
		Vector3 muzzlePos = IsInstanceValid(_muzzle) ? _muzzle.GlobalPosition : rayOrigin;

		// Call RPC across peers
		Rpc(nameof(ShowTracer), muzzlePos, targetPoint);
	}

	private void SetupScopeOverlay()
	{
		if (!HasScopeOverlay || ScopeTexture == null) return;

		_scopeCanvas = new CanvasLayer { Visible = false };
		_scopeRect = new TextureRect
		{
			Texture = ScopeTexture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
		};
		_scopeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		_scopeCanvas.AddChild(_scopeRect);
		AddChild(_scopeCanvas);
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

	public override void _ExitTree()
	{
		if (_isZoomed)
		{
			SetZoomState(false);
			if (IsInstanceValid(_camera)) _camera.Fov = _defaultFov;
		}
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
