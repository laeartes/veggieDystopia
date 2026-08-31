using Godot;
using System;

public partial class ClassSelectUi : Control
{
	[Export] public PlayerClassResource[] AvailableClasses;

	private PlayerClassHandler _classHandler;
	private VBoxContainer _buttonContainer;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect, keepOffsets: false);
		
		// Find Player root, then grab PlayerClassHandler
		Player player = GetNodeAncestor<Player>(this);
		if (player != null)
		{
			_classHandler = player.GetNodeOrNull<PlayerClassHandler>("PlayerClassHandler");
		}

		_buttonContainer = GetNodeOrNull<VBoxContainer>("VBoxContainer");

		if (_classHandler == null)
			GD.PrintErr("[ClassSelectUi] ERR: PlayerClassHandler not found on Player!");
		if (_buttonContainer == null)
			GD.PrintErr("[ClassSelectUi] ERR: VBoxContainer missing from UI tree!");

		GenerateClassButtons();
		Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.M)
			{
				ToggleMenu(!Visible);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	

	public void ToggleMenu(bool show)
	{
		Visible = show;
		Input.MouseMode = show ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
		GD.Print($"[ClassSelectUi] Menu toggled. Visible: {Visible}");
	}

	private void GenerateClassButtons()
	{
		if (_buttonContainer == null || AvailableClasses == null) return;

		foreach (Node child in _buttonContainer.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var classResource in AvailableClasses)
		{
			if (classResource == null) continue;

			Button btn = new Button 
			{ 
				Text = classResource.ClassName,
				CustomMinimumSize = new Vector2(200, 50)
			};
			
			PlayerClassResource res = classResource;
			btn.Pressed += () => OnButtonPressed(res);
			_buttonContainer.AddChild(btn);
		}
	}

	private void OnButtonPressed(PlayerClassResource selectedClass)
	{
		GD.Print($"[ClassSelectUi] Button pressed: {selectedClass.ClassName}");
		
		// Find the local owning player
		Player localPlayer = GetNodeAncestor<Player>(this);

		if (_classHandler != null)
		{
			_classHandler.ApplyClass(selectedClass);
		}
		else
		{
			GD.PrintErr("[ClassSelectUi] Cannot apply class: _classHandler is null!");
		}

		// Trigger local player class change & respawn logic
		if (localPlayer != null && localPlayer.IsMultiplayerAuthority())
		{
			localPlayer.SelectClass(selectedClass.ClassName);
		}

		// Close menu and re-capture mouse
		ToggleMenu(false);
		Input.MouseMode = Input.MouseModeEnum.Captured;
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
