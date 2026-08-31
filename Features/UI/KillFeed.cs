using Godot;
using System;

public partial class KillFeed : VBoxContainer
{
	[Export] public float MessageDuration = 4.0f;

	public static KillFeed Instance { get; private set; }

	public override void _Ready()
	{
		Player player = GetNodeAncestor<Player>(this);
		
		// Only assign Instance if this HUD belongs to the local player
		if (player != null && player.IsMultiplayerAuthority())
		{
			Instance = this;
		}
	}

	public void AddLog(string attackerName, string victimName)
	{
		Label logLabel = new Label
		{
			Text = $"{attackerName} ➔ {victimName}",
			HorizontalAlignment = HorizontalAlignment.Right
		};

		logLabel.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
		logLabel.AddThemeFontSizeOverride("font_size", 14);

		AddChild(logLabel);

		GetTree().CreateTimer(MessageDuration).Timeout += () => logLabel.QueueFree();
	}

	private T GetNodeAncestor<T>(Node startNode) where T : Node
	{
		Node current = startNode.GetParent();
		while (current != null)
		{
			if (current is T ancestor) return ancestor;
			current = current.GetParent();
		}
		return null;
	}
}
