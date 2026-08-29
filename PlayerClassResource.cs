using Godot;

[GlobalClass]
public partial class PlayerClassResource : Resource
{
	[Export] public string ClassName = "Assault";
	[Export] public float MaxHealth = 100f;
	[Export] public PackedScene PrimaryWeaponPrefab; 
}
