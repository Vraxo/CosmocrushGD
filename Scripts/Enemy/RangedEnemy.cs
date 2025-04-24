using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;

	protected override float ProximityThreshold => 320f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;
	protected override Color ParticleColor => new(163f / 255f, 73f / 255f, 164f / 255f);

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero;
			}
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Vector2 desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

			if (distanceToPlayer > DamageRadius)
			{
				desiredMovement = directionToPlayer * Speed;
			}
			else if (distanceToPlayer < ProximityThreshold)
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
		if (GlobalAudioPlayer.Instance is null || projectileScene is null || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			GD.PrintErr("RangedEnemy: Preconditions not met for shooting.");
			return;
		}

		Projectile projectile = GlobalAudioPlayer.Instance.GetProjectile(projectileScene);
		if (projectile is null)
		{
			GD.PrintErr("RangedEnemy: Failed to get projectile from pool.");
			return;
		}

		Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();

		projectile.Setup(GlobalPosition, direction);
		projectile.Activate();
	}
}
