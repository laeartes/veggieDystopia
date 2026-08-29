using Godot;
using System;

public partial class PlayerHUD : CanvasLayer
{
	private ProgressBar _hpBar;
	private HealthComponent _healthComponent;

	public override void _Ready()
	{
		_hpBar = GetNode<ProgressBar>("HPBar");
		_healthComponent = GetParent().GetNodeOrNull<HealthComponent>("HealthComponent");

		if (_healthComponent != null)
		{
			_healthComponent.HealthChanged += OnHealthChanged;
			
			_hpBar.MaxValue = _healthComponent.MaxHealth;
			_hpBar.Value = _healthComponent.MaxHealth;
		}

		if (GetParent() is Node3D parent3D && !parent3D.IsMultiplayerAuthority())
		{
			Hide();
		}
	}

	private void OnHealthChanged(float currentHealth, float maxHealth)
	{
		_hpBar.Value = currentHealth;
	}
}
