using Godot;

public partial class PlayerAbilityHandler : Node
{
	[Signal] public delegate void AbilitiesInitializedEventHandler(PlayerAbilityHandler handler);

	public Ability TacticalAbility { get; private set; }
	public Ability UltimateAbility { get; private set; }

	private Player _player;

	public override void _Ready()
	{
		_player = GetOwner<Player>() ?? GetParent<Player>();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_player != null && !_player.IsMultiplayerAuthority()) return;

		if (@event.IsActionPressed("ability_tactical"))
		{
			TacticalAbility?.TryActivate();
		}
		else if (@event.IsActionPressed("ability_ultimate"))
		{
			UltimateAbility?.TryActivate();
		}
	}

	public void SetupAbilities(AbilityResource tacticalRes, AbilityResource ultimateRes)
	{
		TacticalAbility?.QueueFree();
		UltimateAbility?.QueueFree();
		TacticalAbility = null;
		UltimateAbility = null;

		TacticalAbility = InstantiateAbility(tacticalRes);
		UltimateAbility = InstantiateAbility(ultimateRes);

		EmitSignal(SignalName.AbilitiesInitialized, this);
	}

	private Ability InstantiateAbility(AbilityResource resource)
	{
		if (resource == null || resource.AbilityScript == null) return null;

		if (resource.AbilityScript.New().As<Ability>() is Ability abilityInstance)
		{
			AddChild(abilityInstance);
			abilityInstance.Initialize(resource, _player);
			return abilityInstance;
		}

		GD.PrintErr($"[PlayerAbilityHandler] Failed to instantiate ability script for {resource.AbilityName}");
		return null;
	}
}
