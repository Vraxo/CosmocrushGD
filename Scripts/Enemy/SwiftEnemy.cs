using Godot;

namespace CosmocrushGD;

public partial class SwiftEnemy : BaseEnemy
{
    protected override float Speed => 150f;
    protected override int MaxHealth => 10;
    protected override float KnockbackResistanceMultiplier => 0.15f; // Slightly less resistant than melee

    [Export] private float meleeKnockbackForce = 450f; // Slightly less knockback force than standard melee

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

        TargetPlayer.TakeDamage(Damage); // Use base Damage

        Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);

        CanShoot = false;
        DamageCooldownTimer.Start(); // Use base AttackCooldown
    }
}