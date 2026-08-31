using Godot;
using System;

public partial class SettingsMenu : Control
{
	private HSlider _sensSlider;
	private Label _sensValueLabel;
	private HSlider _fovSlider;
	private Label _fovValueLabel;

	public override void _Ready()
	{
		Visible = false;

		_sensSlider = GetNodeOrNull<HSlider>("Panel/VBoxContainer/SensContainer/SensSlider");
		_sensValueLabel = GetNodeOrNull<Label>("Panel/VBoxContainer/SensContainer/SensValueLabel");
		_fovSlider = GetNodeOrNull<HSlider>("Panel/VBoxContainer/FovContainer/FovSlider");
		_fovValueLabel = GetNodeOrNull<Label>("Panel/VBoxContainer/FovContainer/FovValueLabel");

		if (_sensSlider != null)
		{
			_sensSlider.MinValue = 0.05f;
			_sensSlider.MaxValue = 2.0f;
			_sensSlider.Step = 0.01f;
			_sensSlider.Value = SettingsManager.Instance.MouseSensitivity;
			_sensSlider.ValueChanged += OnSensChanged;
			UpdateSensLabel(_sensSlider.Value);
		}

		if (_fovSlider != null)
		{
			_fovSlider.MinValue = 60.0f;
			_fovSlider.MaxValue = 120.0f;
			_fovSlider.Step = 1.0f;
			_fovSlider.Value = SettingsManager.Instance.Fov;
			_fovSlider.ValueChanged += OnFovChanged;
			UpdateFovLabel(_fovSlider.Value);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			ToggleMenu();
			GetViewport().SetInputAsHandled();
		}
	}

	public void ToggleMenu()
	{
		Visible = !Visible;

		if (Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	private void OnSensChanged(double value)
	{
		float sens = (float)value;
		SettingsManager.Instance.MouseSensitivity = sens;
		UpdateSensLabel(value);
	}

	private void OnFovChanged(double value)
	{
		float fov = (float)value;
		SettingsManager.Instance.Fov = fov;
		UpdateFovLabel(value);
	}

	private void UpdateSensLabel(double value) => _sensValueLabel?.SetText($"{value:F2}");
	private void UpdateFovLabel(double value) => _fovValueLabel?.SetText($"{value:F0}");
}
