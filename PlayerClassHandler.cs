using Godot;
using System;

public partial class PlayerClassHandler : Node
{
	[Export] public PlayerClassResource DefaultClass;
	[Export] public PlayerClassResource CurrentClass { get; private set; }

	private Player _player;
	private Node3D _weaponHolder;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_weaponHolder = _player.GetNodeOrNull<Node3D>("Head/Camera3D/WeaponHolder");

		if (DefaultClass != null)
		{
			ApplyClass(DefaultClass);
		}
	}

	public void ApplyClass(PlayerClassResource newClass)
	{
		if (_player == null || newClass == null) return;

		CurrentClass = newClass;

		HealthComponent health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (health != null)
		{
			health.SetMaxHealth(CurrentClass.MaxHealth, resetCurrent: true);
		}

		EquipWeapon(CurrentClass.PrimaryWeaponPrefab);
	}

	private void EquipWeapon(PackedScene weaponScene)
	{
		if (_weaponHolder == null || weaponScene == null) return;

		foreach (Node child in _weaponHolder.GetChildren())
		{
			child.QueueFree();
		}

		Node weaponInstance = weaponScene.Instantiate();
		_weaponHolder.AddChild(weaponInstance);
	}
}
