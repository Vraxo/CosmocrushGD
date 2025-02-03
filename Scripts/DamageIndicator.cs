using Godot;

namespace CosmocrushGD;

public partial class DamageIndicator : Label
{
    public int Health { get; set; } = 0;
    public int MaxHealth { get; set; } = 0;

    [Export] private Timer timer;
    [Export] private AnimationPlayer player;

    private float speed = 100;
    private float animatedAlpha = 1.0f;

    [Export]
    public float AnimatedAlpha
    {
        get => animatedAlpha;
        set
        {
            animatedAlpha = Mathf.Clamp(value, 0f, 1f);
            UpdateAlpha();
        }
    }

    public override void _Ready()
    {
        timer.Timeout += OnTimerTimeout;
        UpdateOutlineColor();
        player.Play("DamageIndicator");
    }

    public override void _Process(double delta)
    {
        float movement = speed * (float)delta;
        Position = new(Position.X, Position.Y - movement);
    }

    private void UpdateOutlineColor()
    {
        if (MaxHealth <= 0)
        {
            AddThemeColorOverride("font_outline_color", Colors.White);
            return;
        }

        float ratio = Mathf.Clamp((float)Health / MaxHealth, 0f, 1f);

        Color outlineColor = Color.FromHsv(
            Mathf.Lerp(0f, 0.333f, ratio),
            1f,
            1f
        );

        AddThemeColorOverride("font_outline_color", outlineColor);
    }

    private void UpdateAlpha()
    {
        Modulate = new Color(1, 1, 1, AnimatedAlpha);
    }

    private void OnTimerTimeout()
    {
        QueueFree();
    }
}