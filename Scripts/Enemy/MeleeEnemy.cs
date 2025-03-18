using Godot;

namespace CosmocrushGD;

public partial class MeleeEnemy : BaseEnemy
{
    [Export] private float MeleeKnockbackForce = 500f; // Adjust as needed

    protected override void AttemptAttack()
    {
        if (!CanShoot || TargetPlayer == null)
        {
            return;
        }

        float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

        if (distance > DamageRadius)
        {
            return;
        }

        TargetPlayer.TakeDamage(Damage);
        // Apply knockback away from the enemy
        Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);

        CanShoot = false;
        DamageCooldownTimer.Start();
    }
}