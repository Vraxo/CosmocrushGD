using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    public Vector2 Direction = Vector2.Zero;

    private bool active = true;

    [Export] private Sprite2D sprite;
    [Export] private CpuParticles2D destructionParticles;

    private const float Speed = 300f;
    private const float KnockbackForce = 300f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        // Set up automatic destruction after 1 second if not already destroyed
        GetTree().CreateTimer(10.0).Timeout += () =>
        {
            if (active)
            {
                StartDestructionSequence();
            }
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!active)
        {
            return;
        }

        if (Direction != Vector2.Zero)
        {
            GlobalPosition += Direction * Speed * (float)delta;
        }
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
    }

    private void StartDestructionSequence()
    {
        if (!active)
        {
            return;
        }

        active = false;

        sprite.Visible = false;

        Direction = Vector2.Zero;
        Monitoring = false;
        Monitorable = false;

        if (destructionParticles is not null)
        {
            destructionParticles.Emitting = true;
        }

        GetTree().CreateTimer(1.0).Timeout += QueueFree;
    }
}