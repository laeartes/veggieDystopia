using Godot;
using System;

public partial class KillFeed : VBoxContainer
{
	[Export] public float MessageDuration = 4.0f;

	public static KillFeed Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void AddLog(string attackerName, string victimName)
	{
		Label logLabel = new Label
		{
			Text = $"{attackerName} -> {victimName}",
			HorizontalAlignment = HorizontalAlignment.Right
		};

		logLabel.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
		logLabel.AddThemeFontSizeOverride("font_size", 14);

		AddChild(logLabel);

		// Despawn log line
		GetTree().CreateTimer(MessageDuration).Timeout += () => logLabel.QueueFree();
	}
}
