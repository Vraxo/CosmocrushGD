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
        GetTree().CreateTimer(10.0).Timeout += () =>
        {
            if (active) StartDestructionSequence();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!active || Direction == Vector2.Zero)
            return;

        GlobalPosition += Direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!active || body is not Player player)
            return;

        player.TakeDamage(1);
        player.ApplyKnockback(Direction * KnockbackForce);
        StartDestructionSequence();
    }

    private void StartDestructionSequence()
    {
        if (!active) return;
        active = false;

        // Use deferred calls for physics properties
        this.SetDeferred(Area2D.PropertyName.Monitoring, false);
        this.SetDeferred(Area2D.PropertyName.Monitorable, false);

        // Hide visual components
        if (sprite != null)
            sprite.Visible = false;

        // Stop movement
        Direction = Vector2.Zero;

        // Start particles
        if (destructionParticles != null)
        {
            destructionParticles.Emitting = true;
            // Ensure particles complete before freeing
            destructionParticles.OneShot = true;
        }

        // Queue free after delay using SceneTreeTimer
        GetTree().CreateTimer(1.0).Timeout += () =>
        {
            if (IsInstanceValid(this))
                QueueFree();
        };
    }
}