using System.Diagnostics;
using System.Xml.Linq;
using System;
using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    private const float Speed = 300f;
    private const float KnockbackForce = 300f;
    private const float DefaultLifetime = 10.0f;
    private const float DestructionDuration = 0.6f;
    public const int ProjectileZIndex = 5;
    private const uint DefaultProjectileCollisionLayer = 16;
    private const uint DefaultProjectileCollisionMask = 1;

    // private int baseParticleAmount; // No longer needed with simplified reset
    private Timer lifeTimer;
    private Timer destructionTimer;
    private bool active = false;

    [Export] public Sprite2D Sprite;
    [Export] public CpuParticles2D DestructionParticles;

    public Vector2 Direction { get; private set; } = Vector2.Zero;
    public PackedScene SourceScene { get; set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
        if (lifeTimer is null)
        {
            lifeTimer = new Timer { Name = "LifeTimer", OneShot = true };
            AddChild(lifeTimer);
        }
        if (!lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
        {
            lifeTimer.Timeout += OnLifeTimerTimeout;
        }

        destructionTimer = GetNodeOrNull<Timer>("DestructionTimer");
        if (destructionTimer is null)
        {
            destructionTimer = new Timer { Name = "DestructionTimer", OneShot = true };
            AddChild(destructionTimer);
        }
        if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
        {
            destructionTimer.Timeout += ReturnToPool;
        }

        this.CollisionLayer = DefaultProjectileCollisionLayer;
        this.CollisionMask = DefaultProjectileCollisionMask;

        // if (DestructionParticles is not null) // baseParticleAmount capture no longer needed
        // {
        // baseParticleAmount = DestructionParticles.Amount;
        // }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!active || Direction == Vector2.Zero)
        {
            return;
        }

        var movement = Direction * Speed * (float)delta;
        GlobalPosition += movement;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(this))
        {
            BodyEntered -= OnBodyEntered;
        }

        if (lifeTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)) ?? false)
        {
            lifeTimer.Timeout -= OnLifeTimerTimeout;
        }

        if (destructionTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)) ?? false)
        {
            destructionTimer.Timeout -= ReturnToPool;
        }

        base._ExitTree();
    }

    public void SetupAndActivate(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
    {
        if (active)
        {
            return;
        }

        GlobalPosition = startPosition;
        Direction = direction.Normalized();

        if (Sprite is not null)
        {
            if (spriteTexture is not null)
            {
                Sprite.Texture = spriteTexture;
            }
            Sprite.Visible = true;
        }

        if (DestructionParticles is not null)
        {
            DestructionParticles.Emitting = false;
            if (particleColor.HasValue)
            {
                DestructionParticles.Modulate = particleColor.Value;
            }
            else
            {
                DestructionParticles.Modulate = Colors.White;
            }
            DestructionParticles.Position = Vector2.Zero;
        }

        lifeTimer?.Stop();
        destructionTimer?.Stop();

        active = true;
        Visible = true;
        ProcessMode = ProcessModeEnum.Pausable;

        this.CollisionLayer = DefaultProjectileCollisionLayer;
        this.CollisionMask = DefaultProjectileCollisionMask;
        SetDeferred(PropertyName.Monitoring, true);
        SetDeferred(PropertyName.Monitorable, true);

        lifeTimer?.Start(DefaultLifetime);
    }

    public void ResetForPooling()
    {
        GD.Print($"Projectile: Resetting instance {GetInstanceId()} for pooling.");
        active = false;

        lifeTimer?.Stop();
        destructionTimer?.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        this.CollisionLayer = DefaultProjectileCollisionLayer;
        this.CollisionMask = DefaultProjectileCollisionMask;
        SetDeferred(PropertyName.Monitoring, false);
        SetDeferred(PropertyName.Monitorable, false);

        if (Sprite is not null)
        {
            Sprite.Visible = false;
            Sprite.Texture = null;
        }

        if (DestructionParticles is not null)
        {
            DestructionParticles.Restart(); // Re-trigger one-shot emission cycle (clears previous)
            DestructionParticles.Emitting = false; // Ensure it's not left emitting
            DestructionParticles.Modulate = Colors.White;
            DestructionParticles.SpeedScale = 1.0f;
            DestructionParticles.Explosiveness = 1.0f;
        }

        Direction = Vector2.Zero;
        GlobalPosition = Vector2.Zero;
    }

    private void StartDestructionSequence()
    {
        GD.Print($"Projectile: Instance {GetInstanceId()} starting destruction sequence.");
        if (!active)
        {
            return;
        }

        active = false;
        lifeTimer?.Stop();

        CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
        CallDeferred(CanvasItem.MethodName.SetVisible, false);
        SetDeferred(PropertyName.Monitoring, false);
        SetDeferred(PropertyName.Monitorable, false);
        this.CollisionLayer = 0;
        this.CollisionMask = 0;

        Direction = Vector2.Zero;

        if (DestructionParticles is not null)
        {
            DestructionParticles.GlobalPosition = this.GlobalPosition;
            DestructionParticles.Restart();
        }

        if (destructionTimer is not null)
        {
            destructionTimer.Start(DestructionDuration);
        }
        else
        {
            GetTree().CreateTimer(DestructionDuration, false, true).Timeout += ReturnToPool;
        }
    }

    private void ReturnToPool()
    {
        if (DestructionParticles is not null)
        {
            DestructionParticles.Emitting = false;
        }

        destructionTimer?.Stop();

        var poolManager = ProjectilePoolManager.Instance;

        if (poolManager is null)
        {
            GD.PrintErr($"Projectile {GetInstanceId()}: PoolManager instance null on ReturnToPool. Freeing.");
            QueueFree();
            return;
        }

        poolManager.ReturnProjectileToPool(this);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!active)
        {
            return;
        }

        if (body is Player player)
        {
            player.TakeDamage(1);
            player.ApplyKnockback(Direction * KnockbackForce);
            StartDestructionSequence();
        }
        else if (body is BaseEnemy enemy)
        {

        }
        else if (body.IsInGroup("Obstacles") || body is StaticBody2D)
        {
            StartDestructionSequence();
        }
    }

    private void OnLifeTimerTimeout()
    {
        if (!active)
        {
            return;
        }
        StartDestructionSequence();
    }
}