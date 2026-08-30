using Godot;

public partial class AbilitySlotUI : Control
{
	[Export] private TextureRect _iconRect;
	[Export] private TextureProgressBar _cooldownProgress;
	[Export] private Label _cooldownLabel;
	[Export] private Label _keyLabel;

	private Ability _trackedAbility;

	public void Setup(Ability ability, string keyPrompt = "")
	{
		_trackedAbility = ability;
		Visible = true;

		if (_keyLabel != null && !string.IsNullOrEmpty(keyPrompt))
		{
			_keyLabel.Text = keyPrompt;
		}

		if (_iconRect != null && _trackedAbility?.ResourceData?.Icon != null)
		{
			_iconRect.Texture = _trackedAbility.ResourceData.Icon;
		}
	}

	public override void _Process(double delta)
	{
		if (_trackedAbility == null || !IsInstanceValid(_trackedAbility))
		{
			if (_cooldownLabel != null) _cooldownLabel.Text = "";
			return;
		}

		float maxCd = _trackedAbility.ResourceData?.Cooldown ?? 1f;
		float currentCd = _trackedAbility.CurrentCooldown;

		if (currentCd > 0f)
		{
			if (_cooldownProgress != null)
			{
				_cooldownProgress.Value = (currentCd / maxCd) * 100f;
			}
			if (_cooldownLabel != null)
			{
				_cooldownLabel.Text = currentCd.ToString("0.0");
			}
		}
		else
		{
			if (_cooldownProgress != null) _cooldownProgress.Value = 0f;
			if (_cooldownLabel != null) _cooldownLabel.Text = "";
		}
	}
}
