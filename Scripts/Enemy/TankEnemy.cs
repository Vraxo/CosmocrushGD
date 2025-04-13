using Godot;

namespace CosmocrushGD;

public partial class TankEnemy : BaseEnemy
{
    protected override int MaxHealth => 100;
    protected override float Speed => 50f;
    protected override int Damage => 1;
    protected override float KnockbackRecovery => 0.05f;
    protected override float KnockbackResistanceMultiplier => 0.3f; // Takes only 30% of knockback

    private const float MeleeKnockbackForce = 100f;

    protected override void AttemptAttack()
    {
        if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
        {
            return;
        }

        float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

        if (distance > DamageRadius)
        {
            return;
        }

        TargetPlayer.TakeDamage(Damage);

        Vector2 knockbackDirection = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDirection * MeleeKnockbackForce);

        CanShoot = false;
        DamageCooldownTimer.Start();
    }
}