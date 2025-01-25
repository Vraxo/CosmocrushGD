using Godot;

public partial class DamageIndicator : Label
{
    public int Health { get; set; } = 0;
    public int MaxHealth { get; set; } = 0;

    [Export] private Timer timer;
    [Export] private AnimationPlayer player;

    private float speed = 100;
    private Color _baseColor = Colors.White;
    private float _animatedAlpha = 1.0f;

    [Export]
    public float AnimatedAlpha
    {
        get => _animatedAlpha;
        set
        {
            _animatedAlpha = Mathf.Clamp(value, 0f, 1f);
            UpdateModulate();
        }
    }

    public override void _Ready()
    {
        timer.Timeout += OnTimerTimeout;
        UpdateColorBasedOnHealth();
        player.Play("DamageIndicator");
    }

    public override void _Process(double delta)
    {
        float movement = speed * (float)delta;
        Position = new(Position.X, Position.Y - movement);
    }

    private void UpdateColorBasedOnHealth()
    {
        if (MaxHealth <= 0)
        {
            _baseColor = Colors.White;
            UpdateModulate();
            return;
        }

        float ratio = Mathf.Clamp((float)Health / MaxHealth, 0f, 1f);
        _baseColor = Color.FromHsv(
            Mathf.Lerp(0f, 0.333f, ratio), // Hue transition (red to green)
            1f,  // Full saturation
            1f   // Full brightness
        );
        UpdateModulate();
    }

    private void UpdateModulate()
    {
        // Combine health color with animation alpha
        Modulate = new(_baseColor.R, _baseColor.G, _baseColor.B, AnimatedAlpha);
    }

    private void OnTimerTimeout()
    {
        QueueFree();
    }
}