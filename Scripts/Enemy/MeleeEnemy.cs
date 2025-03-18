namespace CosmocrushGD;

public partial class MeleeEnemy : BaseEnemy
{
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
        CanShoot = false;
        DamageCooldownTimer.Start();
    }
}