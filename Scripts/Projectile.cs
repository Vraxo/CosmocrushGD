using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    public Vector2 Direction = Vector2.Zero;

    [Export] public Sprite2D Sprite;
    [Export] public CpuParticles2D DestructionParticles;

    private bool active = true;

    private const float Speed = 300f;
    private const float KnockbackForce = 300f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        
        GetTree().CreateTimer(10.0).Timeout += () =>
        {
            if (!active)
            {
                return;
            }

            StartDestructionSequence();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!active || Direction == Vector2.Zero)
        {
            return;
        }

        GlobalPosition += Direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!active || body is not Player player)
        {
            return;
        }

        player.TakeDamage(1);
        player.ApplyKnockback(Direction * KnockbackForce);
        StartDestructionSequence();
    }

    private void StartDestructionSequence()
    {
        if (!active)
        {
            return;
        }

        active = false;

        SetDeferred(Area2D.PropertyName.Monitoring, false);
        SetDeferred(Area2D.PropertyName.Monitorable, false);

        if (Sprite != null)
        {
            Sprite.Visible = false;
        }

        Direction = Vector2.Zero;

        if (DestructionParticles is not null)
        {
            DestructionParticles.Emitting = true;
            DestructionParticles.OneShot = true;
        }

        GetTree().CreateTimer(1.0).Timeout += () =>
        {
            if (!IsInstanceValid(this))
            {
                return;
            }

            QueueFree();
        };
    }
}