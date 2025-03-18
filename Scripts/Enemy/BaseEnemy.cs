using Godot;

namespace CosmocrushGD;

public abstract partial class BaseEnemy : CharacterBody2D
{
    [Export] protected NavigationAgent2D Navigator;
    [Export] protected Sprite2D Sprite;
    [Export] protected Timer DeathTimer;
    [Export] protected Timer DamageCooldownTimer;
    [Export] protected CollisionShape2D Collider;
    [Export] protected CpuParticles2D DamageParticles;
    [Export] protected CpuParticles2D DeathParticles;
    [Export] protected PackedScene DamageIndicatorScene;
    [Export] protected AnimationPlayer HitAnimationPlayer;

    protected bool Dead = false;
    protected bool CanShoot = true;
    protected int Health;
    protected Vector2 Knockback = Vector2.Zero;
    protected Player TargetPlayer;

    protected virtual int MaxHealth => 20;
    protected virtual int Damage => 1;
    protected virtual float Speed => 100f;
    protected virtual float DamageRadius => 50f;
    protected virtual float ProximityThreshold => 32f;
    protected virtual float KnockbackRecovery => 0.1f;
    protected virtual float AttackCooldown => 0.5f;

    public override void _Ready()
    {
        TargetPlayer = GetNode<Player>("/root/World/Player");
        Health = MaxHealth;

        DeathTimer.Timeout += OnDeathTimeout;
        DamageCooldownTimer.WaitTime = AttackCooldown;
        DamageCooldownTimer.Timeout += () => CanShoot = true;
    }

    public override void _Process(double delta)
    {
        if (Dead)
        {
            return;
        }

        UpdateSpriteDirection();
        AttemptAttack();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Dead)
        {
            return;
        }

        Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
        Velocity = CalculateMovement() + Knockback;
        MoveAndSlide();
    }

    protected virtual Vector2 CalculateMovement()
    {
        if (TargetPlayer == null) return Vector2.Zero;

        // Check if within proximity threshold to stop moving closer
        float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
        if (distanceToPlayer <= ProximityThreshold)
        {
            return Vector2.Zero;
        }

        Navigator.TargetPosition = TargetPlayer.GlobalPosition;

        // Get the navigation map RID through the NavigationServer
        Rid mapRid = NavigationServer2D.AgentGetMap(Navigator.GetRid());

        if (!mapRid.IsValid)
        {
            // No valid navigation map associated with the agent
            return Vector2.Zero;
        }

        // Check if the map has completed synchronization
        long iterationId = NavigationServer2D.MapGetIterationId(mapRid);
        if (iterationId <= 0)
        {
            // Map hasn't synchronized yet
            return Vector2.Zero;
        }

        // Safe to get path position now
        Vector2 direction = (Navigator.GetNextPathPosition() - GlobalPosition).Normalized();
        return direction * Speed;
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        HitAnimationPlayer.Play("HitFlash");
        ShowDamageIndicator(damage);
        DamageParticles.Emitting = true;

        if (Health <= 0) Die();
    }

    public void ApplyKnockback(Vector2 force)
    {
        if (Knockback.Length() < force.Length())
        {
            Knockback = force;
        }
        else
        {
            Knockback += force;
        }
    }

    protected virtual void UpdateSpriteDirection()
    {
        if (TargetPlayer is null)
        {
            return;
        }

        Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
    }

    protected abstract void AttemptAttack();

    protected virtual void Die()
    {
        Dead = true;
        Collider.Disabled = true;
        Sprite.Visible = false;
        DeathParticles.Emitting = true;
        DeathTimer.Start();
    }

    private void ShowDamageIndicator(int damage)
    {
        var indicator = DamageIndicatorScene.Instantiate<DamageIndicator>();
        indicator.Text = damage.ToString();
        indicator.Position = new Vector2(0, -Sprite.Texture.GetHeight() / 2);
        AddChild(indicator);
    }

    private void OnDeathTimeout() => QueueFree();
}