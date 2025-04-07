using Godot;

namespace CosmocrushGD;

public partial class MeleeEnemy : BaseEnemy
{
    [Export] private float meleeKnockbackForce = 500f;

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

        Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);

        CanShoot = false;
        DamageCooldownTimer.Start();
    }
}