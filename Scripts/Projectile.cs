using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
    [Export] public float Speed = 300f;
    public Vector2 Direction = Vector2.Zero;

    public override void _PhysicsProcess(double delta)
    {
        if (Direction != Vector2.Zero)
        {
            GlobalPosition += Direction * Speed * (float)delta;
        }
    }
}