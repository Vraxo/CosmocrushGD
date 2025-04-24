using Godot;

namespace CosmocrushGD;

public partial class MeleeEnemy : BaseEnemy
{
    [Export] private float meleeKnockbackForce = 500f;

    protected override Color ParticleColor => new(237f / 255f, 28f / 255f, 36f / 255f);
    protected override float MeleeKnockbackForce => meleeKnockbackForce;

    protected override void PerformAttackAction()
    {
        if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
        {
            return;
        }

        TargetPlayer.TakeDamage(Damage);

        Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
        TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);
    }
}