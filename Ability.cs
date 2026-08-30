using Godot;

public abstract partial class Ability : Node
{
	public AbilityResource ResourceData { get; private set; }
	public Player OwnerPlayer { get; private set; }
	
	public float CurrentCooldown { get; private set; } = 0f;
	public bool IsOnCooldown => CurrentCooldown > 0f;

	public void Initialize(AbilityResource data, Player player)
	{
		ResourceData = data;
		OwnerPlayer = player;
	}

	public override void _Process(double delta)
	{
		if (CurrentCooldown > 0f)
		{
			CurrentCooldown = Mathf.Max(0f, CurrentCooldown - (float)delta);
		}
	}

	public bool CanActivate()
	{
		return !IsOnCooldown && IsInstanceValid(OwnerPlayer);
	}

	public bool TryActivate()
	{
		if (!CanActivate()) return false;

		Execute();
		CurrentCooldown = ResourceData?.Cooldown ?? 0f;
		return true;
	}

	protected abstract void Execute();
}
