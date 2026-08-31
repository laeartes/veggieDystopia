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

		_player.MaxSpeed = CurrentClass.MaxSpeed;
		_player.CrouchSpeed = CurrentClass.CrouchSpeed;
		_player.JumpForce = CurrentClass.JumpForce;
		_player.Accel = CurrentClass.Accel;

		PlayerAbilityHandler abilityHandler = _player.GetNodeOrNull<PlayerAbilityHandler>("PlayerAbilityHandler");
		if (abilityHandler != null)
		{
			abilityHandler.SetupAbilities(newClass.TacticalAbility, newClass.UltimateAbility);
		}

		// Call RPC across all peers so node trees match everywhere
		if (newClass.PrimaryWeaponPrefab != null)
		{
			Rpc(nameof(RpcEquipWeapon), newClass.PrimaryWeaponPrefab.ResourcePath);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcEquipWeapon(string scenePath)
	{
		if (_weaponHolder == null) return;

		foreach (Node child in _weaponHolder.GetChildren())
		{
			child.QueueFree();
		}

		PackedScene weaponScene = GD.Load<PackedScene>(scenePath);
		if (weaponScene == null) return;

		Node weaponInstance = weaponScene.Instantiate();
		// Give explicit static name to keep NodePaths identical across all clients
		weaponInstance.Name = "CurrentWeapon"; 
		_weaponHolder.AddChild(weaponInstance);
	}
}
