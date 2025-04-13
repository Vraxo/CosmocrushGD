using Godot;

namespace CosmocrushGD;

public partial class TankEnemy : BaseEnemy
{
    // Override base properties for Tank behavior
    protected override int MaxHealth => 80; // More health
    protected override float Speed => 60f; // Slower speed
    protected override int Damage => 2; // Higher damage

    // *** This is the important part for the CS0115 fix ***
    // Override the virtual property from BaseEnemy
    // A value less than 1.0 reduces knockback (e.g., 0.1 means 10% knockback)
    protected override float KnockbackResistanceMultiplier => 0.1f;

    [Export] private float meleeKnockbackForce = 600f; // Can have its own knockback force if desired

    protected override void AttemptAttack()
    {
        // Standard melee attack logic (same as MeleeEnemy for this example)
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

        Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);

        CanShoot = false;
        DamageCooldownTimer.Start();
    }

    // Add any other Tank-specific logic or overrides here
}