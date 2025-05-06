using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;

	protected override float ProximityThreshold => 280f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;
	protected override Color ParticleColor => new(0.64f, 0.29f, 0.64f);

	private static readonly Color BaseRangedProjectileColor = new(163f / 255f, 73f / 255f, 164f / 255f);
	private const float ProjectileGlowIntensity = 2.5f; // Increased intensity for purple
	private static readonly Color RangedProjectileColor = new(
		BaseRangedProjectileColor.R * ProjectileGlowIntensity,
		BaseRangedProjectileColor.G * ProjectileGlowIntensity,
		BaseRangedProjectileColor.B * ProjectileGlowIntensity
	);

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Dead)
		{
			return;
		}

		var desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			var directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			var distanceToPlayerSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);

			if (distanceToPlayerSq > DamageRadius * DamageRadius)
			{
				desiredMovement = directionToPlayer * Speed;
			}
			else if (distanceToPlayerSq < ProximityThreshold * ProximityThreshold)
			{
				desiredMovement = -directionToPlayer * Speed;
			}
		}

		Velocity = desiredMovement + Knockback;

		if (Velocity.LengthSquared() > 0.01f)
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero)
		{
			Velocity = Vector2.Zero;
		}
	}

	protected override void PerformAttackAction()
	{
		ShootProjectile();
	}

	private void ShootProjectile()
	{
		if (projectileScene is null || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			GD.PrintErr($"RangedEnemy ({Name}): Preconditions not met for shooting. Scene: {projectileScene}, Target: {TargetPlayer?.Name ?? "null"}");
			return;
		}

		var projectile = projectileScene.Instantiate<Projectile>();
		if (projectile is null)
		{
			GD.PrintErr($"RangedEnemy ({Name}): Failed to instantiate projectile from scene.");
			return;
		}

		GetTree().Root.AddChild(projectile);

		var direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		var projectileTexture = Sprite?.Texture;

		projectile.SetupAndActivate(GlobalPosition, direction, projectileTexture, RangedProjectileColor);
		GD.Print($"RangedEnemy ({Name}): Fired projectile.");
	}
}
