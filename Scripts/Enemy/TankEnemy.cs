using Godot;

namespace CosmocrushGD;

public partial class TankEnemy : BaseEnemy
{
    protected override int MaxHealth => 80;
    protected override float Speed => 60f;
    protected override int Damage => 2;
    protected override float KnockbackResistanceMultiplier => 0.1f;
    protected override Color ParticleColor => new(185f / 255f, 122f / 255f, 87f / 255f);

    [Export] private float meleeKnockbackForce = 600f;

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