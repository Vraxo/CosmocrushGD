using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    [Export] public float Speed = 300f;
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
        // Check if the collided body is the Player
        if (body is Player player)
        {
            // Apply damage and destroy the projectile
            player.TakeDamage(1);
            QueueFree(); // Destroy the projectile
        }
    }
}