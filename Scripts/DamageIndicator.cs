using Godot;

namespace CosmocrushGD;

public partial class DamageIndicator : Label
{
    public int Health { get; set; } = 0;
    public int MaxHealth { get; set; } = 0;

    [Export] private Timer destructionTimer;
    [Export] private AnimationPlayer player;

    private const float Speed = 100;

    private float _animatedAlpha = 1.0f;
    public float AnimatedAlpha
    {
        get => _animatedAlpha;
        set
        {
            _animatedAlpha = Mathf.Clamp(value, 0f, 1f);
            UpdateAlpha();
        }
    }

    public override void _Ready()
    {
        destructionTimer.Timeout += OnTimerTimeout;
        SetOutlineColor();
        player.Play("DamageIndicator");
    }

    public override void _Process(double delta)
    {
        float movement = Speed * (float)delta;
        Position = new(Position.X, Position.Y - movement);
    }

    private void SetOutlineColor()
    {
        if (MaxHealth <= 0)
        {
            AddThemeColorOverride("font_color", Colors.White);
            return;
        }

        float ratio = Mathf.Clamp((float)Health / MaxHealth, 0f, 1f);

        var outlineColor = Color.FromHsv(
            Mathf.Lerp(0f, 0.333f, ratio),
            1f,
            1f
        );

        AddThemeColorOverride("font_color", outlineColor);
    }

    private void UpdateAlpha()
    {
        Modulate = new(1, 1, 1, AnimatedAlpha);
    }

    private void OnTimerTimeout()
    {
        QueueFree();
    }
}