using Godot;

public partial class PlayerHUD : CanvasLayer
{
    private ProgressBar _hpBar;
    private HealthComponent _healthComponent;

    [Export] private AbilitySlotUI _tacticalSlot;
    [Export] private AbilitySlotUI _ultimateSlot;

	public override void _Ready()
	{
		_hpBar = GetNodeOrNull<ProgressBar>("HPBar");
		
		Node parent = GetParent();
		if (parent != null)
		{
			_healthComponent = parent.GetNodeOrNull<HealthComponent>("HealthComponent");

			if (parent is Node3D parent3D && !parent3D.IsMultiplayerAuthority())
			{
				Hide();
				return;
			}
		}

		if (_healthComponent != null && _hpBar != null)
		{
			_healthComponent.HealthChanged += OnHealthChanged;
			
			// Explicit initial sync
			OnHealthChanged(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
		}

		if (parent != null)
		{
			PlayerAbilityHandler abilityHandler = parent.GetNodeOrNull<PlayerAbilityHandler>("PlayerAbilityHandler");
			if (abilityHandler != null)
			{
				abilityHandler.AbilitiesInitialized += BindAbilities;
				BindAbilities(abilityHandler);
			}
		}
	}

    public void BindAbilities(PlayerAbilityHandler abilityHandler)
    {
        if (abilityHandler == null) return;

        if (_tacticalSlot != null) 
            _tacticalSlot.Setup(abilityHandler.TacticalAbility, "E");
            
        if (_ultimateSlot != null) 
            _ultimateSlot.Setup(abilityHandler.UltimateAbility, "Q");
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (_hpBar != null)
        {
            _hpBar.MaxValue = maxHealth;
            _hpBar.Value = currentHealth;
        }
    }
}