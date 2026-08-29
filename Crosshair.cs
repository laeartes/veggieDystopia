using Godot;
using System;

public partial class Crosshair : Control
{
	[Export] public float BaseGap = 2.0f;
	[Export] public float MaxGap = 16.0f;
	[Export] public float LineLength = 4.0f;
	[Export] public float LineThickness = 2.0f;
	
	[Export] public Color CrosshairColor = new Color(0f, 1f, 1f, 1f); 
	[Export] public Color BorderColor = new Color(0f, 0f, 0f, 0.8f);

	[Export] public Font LabelFont;
	[Export] public int FontSize = 13;

	private float _currentGap = 2.0f;
	private float _currentSpreadValue = 0f;
	private Player _ownerPlayer;
	private HitscanWeapon _equippedWeapon;

	public override void _Ready()
	{
		_ownerPlayer = GetNodeAncestor<Player>(this);

		// Force full screen rect and clear all margins
		SetAnchorsPreset(LayoutPreset.FullRect, keepOffsets: false);
		OffsetLeft = 0;
		OffsetTop = 0;
		OffsetRight = 0;
		OffsetBottom = 0;
		
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (_ownerPlayer != null && !_ownerPlayer.IsMultiplayerAuthority()) return;

		// Fetch weapon dynamically if null
		if (_equippedWeapon == null && _ownerPlayer != null)
		{
			_equippedWeapon = FindChildOfType<HitscanWeapon>(_ownerPlayer);
		}

		if (_equippedWeapon != null)
		{
			_currentSpreadValue = _equippedWeapon.CurrentSpread;
		}

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

		Vector2 screenCenter = GetViewportRect().Size / 2.0f;
		Vector2[] directions = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };

		// 1. Draw Reticle
		foreach (Vector2 dir in directions)
		{
			Vector2 start = screenCenter + dir * _currentGap;
			Vector2 end = start + dir * LineLength;

			DrawLine(start, end, BorderColor, LineThickness + 2.0f);
			DrawLine(start, end, CrosshairColor, LineThickness);
		}

		// 2. Draw Live Spread Value
		string text = $"SPREAD: {Mathf.RadToDeg(_currentSpreadValue):F2}°";
		Vector2 textPosition = screenCenter + new Vector2(-40.0f, _currentGap + LineLength + 16.0f);
		
		DrawString(
			LabelFont ?? ThemeDB.FallbackFont,
			textPosition,
			text,
			HorizontalAlignment.Center,
			80,
			FontSize,
			CrosshairColor
		);
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

	private T FindChildOfType<T>(Node parent) where T : Node
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is T typedChild) return typedChild;
			T recursiveResult = FindChildOfType<T>(child);
			if (recursiveResult != null) return recursiveResult;
		}
		return null;
	}
}
