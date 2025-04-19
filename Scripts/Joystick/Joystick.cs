using Godot;

public partial class Joystick : Control
{
    [Export] public bool SimulateInput = true;
    [Export] public float Deadzone = 0.3f;

    public Vector2 PosVector = Vector2.Zero;

    public Vector2 Direction
    {
        get
        {
            float length = PosVector.Length();

            if (length < Deadzone)
            {
                return Vector2.Zero;
            }

            float scale = (length - Deadzone) / (1 - Deadzone);
            return PosVector.Normalized() * scale;
        }
    }

    public override void _Ready()
    {
        if (!OS.HasFeature("mobile"))
        {
            QueueFree();
            return;
        }

        Visible = true;
    }
}