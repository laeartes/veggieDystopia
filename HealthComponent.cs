using Godot;
using System;

public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);
	[Signal] public delegate void DiedEventHandler();

	[Export] public float MaxHealth { get; set; } = 100f;
	public float CurrentHealth { get; private set; }

	public override void _EnterTree()
	{
		if (GetParent() is Node parent && int.TryParse(parent.Name, out int peerId))
		{
			SetMultiplayerAuthority(peerId);
		}
	}

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void TakeDamage(float amount)
	{
		if (!IsMultiplayerAuthority() && !Multiplayer.IsServer()) return;

		CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
		
		Rpc(nameof(SyncHealth), CurrentHealth);

		if (CurrentHealth <= 0f)
		{
			Rpc(nameof(HandleDeath));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncHealth(float newHealth)
	{
		CurrentHealth = newHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void HandleDeath()
	{
		EmitSignal(SignalName.Died);
		
		if (Multiplayer.IsServer())
		{
			CurrentHealth = MaxHealth;
			Rpc(nameof(SyncHealth), CurrentHealth);
		}
	}
}
