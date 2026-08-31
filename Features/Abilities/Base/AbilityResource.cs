using Godot;

[GlobalClass]
public partial class AbilityResource : Resource
{
	[Export] public string AbilityName = "Sprint";
	[Export] public string Description = "Increases speed for a short duration.";
	[Export] public float Cooldown = 10f;
	[Export] public Texture2D Icon;
	
	[ExportGroup("Execution Logic")]
	[Export] public CSharpScript AbilityScript;
}
