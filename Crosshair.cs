using Godot;
using System;

public partial class Crosshair : Control
{
	// tried to copy tenz
	[Export] public float BaseGap = 2.0f;        
	[Export] public float MaxGap = 16.0f;       
	[Export] public float LineLength = 4.0f;   
	[Export] public float LineThickness = 2.0f; 
	
	[Export] public Color CrosshairColor = new Color(0f, 1f, 1f, 1f); 
	[Export] public Color BorderColor = new Color(0f, 0f, 0f, 0.8f);

	private float _currentGap = 2.0f;
	private Player _ownerPlayer;

	public override void _Ready()
	{
		_ownerPlayer = GetNodeAncestor<Player>(this);
		
		SetAnchorsPreset(LayoutPreset.Center);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (_ownerPlayer != null && !_ownerPlayer.IsMultiplayerAuthority()) return;

		float targetGap = BaseGap;

		if (_ownerPlayer != null)
		{
			float speed = new Vector3(_ownerPlayer.Velocity.X, 0, _ownerPlayer.Velocity.Z).Length();
			targetGap += (speed / 7.0f) * 8.0f;

			if (!_ownerPlayer.IsOnFloor())
			{
				targetGap += 10.0f;
			}
		}

		targetGap = Mathf.Clamp(targetGap, BaseGap, MaxGap);
		_currentGap = Mathf.Lerp(_currentGap, targetGap, (float)delta * 20f);

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_ownerPlayer != null && !_ownerPlayer.IsMultiplayerAuthority()) return;

		Vector2 center = Vector2.Zero;
		Vector2[] directions = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };

		foreach (Vector2 dir in directions)
		{
			Vector2 start = center + dir * _currentGap;
			Vector2 end = start + dir * LineLength;

			DrawLine(start, end, BorderColor, LineThickness + 2.0f);
			DrawLine(start, end, CrosshairColor, LineThickness);
		}
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
