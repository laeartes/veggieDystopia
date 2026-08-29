using Godot;

[GlobalClass]
public partial class SkillTreeNodeResource : Resource
{
	[Export] public string SkillName = "Health Boost";
	[Export] public string Description = "+20 Max Health";
	[Export] public int Cost = 1;
	[Export] public bool IsUnlocked = false;

	// Prerequisites for unlocking this node in a UI graph
	[Export] public SkillTreeNodeResource[] Prerequisites;

	// Optional stat modifiers applied when unlocked
	[Export] public float BonusHealth = 0f;
	[Export] public float BonusSpeed = 0f;
	[Export] public AbilityResource UnlockedAbility;
}
