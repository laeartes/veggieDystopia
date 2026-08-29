using Godot;
using System;

public partial class TargetDummy : StaticBody3D
{
	private HealthComponent _health;
	private MeshInstance3D _mesh;
	private Material _originalMaterial;
	private Vector3 _initialPosition;

	public override void _Ready()
	{
		_health = GetNode<HealthComponent>("HealthComponent");
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");

		_initialPosition = GlobalPosition;

		if (_mesh.MaterialOverride != null)
		{
			_originalMaterial = _mesh.MaterialOverride;
		}

		_health.HealthChanged += OnHealthChanged;
		_health.Died += OnDied;
	}

	private void OnHealthChanged(float currentHealth, float maxHealth)
	{
		FlashHitEffect();
	}

	private async void FlashHitEffect()
	{
		StandardMaterial3D flashMat = new StandardMaterial3D { AlbedoColor = Colors.White };
		_mesh.MaterialOverride = flashMat;
		
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		
		_mesh.MaterialOverride = _originalMaterial;
	}

	private void OnDied()
	{
		Visible = false;
		GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;

		GetTree().CreateTimer(3.0f).Timeout += Respawn;
	}

	private void Respawn()
	{
		GlobalPosition = _initialPosition;
		Visible = true;
		GetNode<CollisionShape3D>("CollisionShape3D").Disabled = false;
	}
}
