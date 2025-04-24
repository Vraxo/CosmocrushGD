using Godot;

namespace CosmocrushGD;

public partial class SwiftEnemy : BaseEnemy
{
    protected override float Speed => 150f;
    protected override int MaxHealth => 10;
    protected override float KnockbackResistanceMultiplier => 0.8f;
    protected override Color ParticleColor => new(255f / 255f, 242f / 255f, 0f / 255f);

    [Export] private float meleeKnockbackForce = 450f;

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