using Godot;

[GlobalClass]
public partial class PlayerClassResource : Resource
{
	[ExportGroup("Base Stats")]
	[Export] public string ClassName = "Assault";
	[Export] public float MaxHealth = 100f;

	[ExportGroup("Movement Overrides")]
	[Export] public float MaxSpeed = 7.0f;
	[Export] public float CrouchSpeed = 3.5f;
	[Export] public float JumpForce = 6.5f;
	[Export] public float Accel = 5.5f;

	[ExportGroup("Loadout")]
	[Export] public PackedScene PrimaryWeaponPrefab;

	[ExportGroup("Abilities")]
	[Export] public AbilityResource TacticalAbility;
	[Export] public AbilityResource UltimateAbility;

	[ExportGroup("Skill Tree")]
	[Export] public SkillTreeNodeResource[] SkillTreeRoots;
}
