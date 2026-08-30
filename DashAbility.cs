using Godot;

public partial class DashAbility : Ability
{
	[Export] public float DashImpulse = 50.0f;
	protected override void Execute()
	{
		if (OwnerPlayer == null) return;

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		
		Vector3 localDir = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
		Vector3 dashDir = OwnerPlayer.Transform.Basis * localDir;

		if (dashDir == Vector3.Zero)
		{
			dashDir = -OwnerPlayer.Transform.Basis.Z; // Default forward
		}

		dashDir = dashDir.Normalized();

		Vector3 currentVel = OwnerPlayer.Velocity;
		OwnerPlayer.Velocity = new Vector3(dashDir.X * DashImpulse, currentVel.Y, dashDir.Z * DashImpulse);
	}
}
