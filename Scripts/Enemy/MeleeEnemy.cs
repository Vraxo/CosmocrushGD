using Godot;

namespace CosmocrushGD;

public partial class MeleeEnemy : BaseEnemy
{
    [Export] private float meleeKnockbackForce = 500f;

    // Optional: Override base properties if needed
    // protected override int MaxHealth => 25; // Example: Tougher melee enemy
    // protected override float AttackInterval => 0.4f; // Example: Faster attacks

    protected override void AttemptAttack()
    {
        // Use corrected property names from BaseEnemy
        if (!CanAttack || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
        {
            return;
        }

        float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

        // Use corrected property name AttackRadius
        if (distance > AttackRadius)
        {
            return;
        }

        // Use corrected property name BaseDamage
        TargetPlayer.TakeDamage(BaseDamage);

        Vector2 knockbackDirection = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDirection * meleeKnockbackForce);

        // Use corrected property name CanAttack
        CanAttack = false;
        DamageCooldownTimer?.Start();
    }
}