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
		
		_classHandler = GetNodeAncestor<PlayerClassHandler>(this);
		_buttonContainer = GetNodeOrNull<VBoxContainer>("VBoxContainer");

		GenerateClassButtons();
		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Toggle class select UI with 'M' key or ESC
		if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.M))
		{
			ToggleMenu(!Visible);
		}
	}

	public void ToggleMenu(bool show)
	{
		Visible = show;
		Input.MouseMode = show ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
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
			Button btn = new Button { Text = classResource.ClassName };
			PlayerClassResource res = classResource;
			btn.Pressed += () => SelectClass(res);
			_buttonContainer.AddChild(btn);
		}
	}

	private void SelectClass(PlayerClassResource selectedClass)
	{
		_classHandler?.ApplyClass(selectedClass);
		ToggleMenu(false);
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
