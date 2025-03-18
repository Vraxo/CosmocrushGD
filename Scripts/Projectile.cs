using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    [Export] public float Speed = 300f;
    [Export] public float KnockbackForce = 300f; // Adjust as needed
    public Vector2 Direction = Vector2.Zero;

    public override void _Ready()
    {
        // Connect the body entered signal to detect collisions
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Direction == Vector2.Zero)
            return;

        GlobalPosition += Direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.TakeDamage(1);
            // Apply knockback in the projectile's direction
            player.ApplyKnockback(Direction * KnockbackForce);
            QueueFree();
        }
    }
}