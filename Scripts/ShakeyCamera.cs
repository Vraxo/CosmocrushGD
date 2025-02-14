using Godot;

namespace CosmocrushGD;

public partial class ShakeyCamera : Camera2D
{
    [Export] public float ShakeDecay { get; set; } = 5.0f;
    [Export] public Vector2 ShakeMaxOffset { get; set; } = new Vector2(10, 5);
    [Export] public float ShakeMaxRoll { get; set; } = 0.1f;

    private float shakeStrength = 0.0f;
    private float shakeDuration = 0.0f;
    private readonly RandomNumberGenerator rng = new();

    public override void _Ready()
    {
        rng.Randomize();
    }

    public override void _Process(double delta)
    {
        if (shakeStrength <= 0)
        {
            return;
        }

        shakeDuration = Mathf.Max(shakeDuration - (float)delta, 0);
        shakeStrength = Mathf.Lerp(shakeStrength, 0.0f, ShakeDecay * (float)delta);

        if (shakeStrength <= 0.01f || shakeDuration <= 0)
        {
            ResetCamera();
        }
        else
        {
            ApplyShake();
        }
    }

    public void Shake(float strength, float duration)
    {
        if (strength > shakeStrength)
        {
            shakeStrength = strength;
        }

        shakeDuration = duration;
    }

    private void ApplyShake()
    {
        float amount = shakeStrength * shakeStrength;

        Vector2 offset = new(
            rng.RandfRange(-1, 1) * ShakeMaxOffset.X * amount,
            rng.RandfRange(-1, 1) * ShakeMaxOffset.Y * amount
        );

        Offset = offset;
        Rotation = rng.RandfRange(-ShakeMaxRoll, ShakeMaxRoll) * amount;
    }

    private void ResetCamera()
    {
        shakeStrength = 0.0f;
        shakeDuration = 0.0f;
        Offset = Vector2.Zero;
        Rotation = 0.0f;
    }
}