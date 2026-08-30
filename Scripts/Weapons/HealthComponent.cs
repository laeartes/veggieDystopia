using Godot;
using System;

public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);
	[Signal] public delegate void DiedEventHandler();

	[Export] public float MaxHealth { get; set; } = 100f;
	public float CurrentHealth { get; private set; }

	private string _lastAttackerName = "Environment";

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}
	public void SetMaxHealth(float newMaxHealth, bool resetCurrent = true)
	{
		MaxHealth = newMaxHealth;
		if (resetCurrent)
		{
			CurrentHealth = newMaxHealth;
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void TakeDamage(float amount, string attackerName = "Unknown")
	{
		if (!Multiplayer.IsServer()) return;

		_lastAttackerName = attackerName;
		CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
		
		Rpc(nameof(SyncHealth), CurrentHealth);

		if (CurrentHealth <= 0f)
		{
			string victimName = GetParent().Name;
			
			// Server broadcasts kill log to all clients (including host)
			Rpc(nameof(BroadcastKill), _lastAttackerName, victimName);
			Rpc(nameof(HandleDeath));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BroadcastKill(string attacker, string victim)
	{
		if (KillFeed.Instance != null)
		{
			KillFeed.Instance.AddLog(attacker, victim);
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
