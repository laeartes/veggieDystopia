using Godot;
using System;

public partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }

	public event Action<float> OnFovChanged;

	private float _fov = 90.0f;
	public float Fov
	{
		get => _fov;
		set
		{
			_fov = value;
			OnFovChanged?.Invoke(_fov);
		}
	}

	public float MouseSensitivity { get; set; } = 0.3f;

	private const float ValToRadFactor = 0.00122173f;
	public float InternalMouseSensitivity => MouseSensitivity * ValToRadFactor;

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;
	}
}
