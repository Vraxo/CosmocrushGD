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
    private Tween zoomTween;

    public override void _Ready()
    {
        rng.Randomize();
        GD.Print($"ShakeyCamera._Ready: Initial Zoom: {Zoom}");
    }

    public override void _Process(double delta)
    {
        // Print zoom level every frame (even when paused due to ProcessMode=Always)
        // Reduce frequency if too spammy, e.g., using a timer check
        // GD.Print($"ShakeyCamera._Process: Current Zoom: {Zoom}");

        if (shakeStrength <= 0)
        {
            // Only reset shake if not actively shaking
            if (Offset != Vector2.Zero || Rotation != 0.0f)
            {
                // ResetShake(); // Maybe don't reset here if zoom is happening? Let zoom finish?
            }
            // return; // Keep processing zoom even if not shaking
        }
        else // Only apply shake logic if actively shaking
        {
            shakeDuration = Mathf.Max(shakeDuration - (float)delta, 0);
            shakeStrength = Mathf.Lerp(shakeStrength, 0.0f, ShakeDecay * (float)delta);

            if (shakeStrength <= 0.01f || shakeDuration <= 0)
            {
                ResetShake();
            }
            else
            {
                ApplyShake();
            }
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

    public void ZoomToPoint(float zoomAmount, float duration)
    {
        GD.Print($"ShakeyCamera.ZoomToPoint: Called with zoomAmount={zoomAmount}, duration={duration}");
        zoomTween?.Kill();
        GD.Print($"ShakeyCamera.ZoomToPoint: Previous zoomTween killed (if existed).");

        zoomTween = CreateTween();
        if (zoomTween is null)
        {
            GD.PrintErr("ShakeyCamera.ZoomToPoint: Failed to create Tween!");
            return;
        }

        zoomTween.SetParallel(false);
        zoomTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        zoomTween.SetEase(Tween.EaseType.Out);
        zoomTween.SetTrans(Tween.TransitionType.Cubic);

        GD.Print($"ShakeyCamera.ZoomToPoint: Tween created and configured (ProcessMode=Idle). Binding property...");
        zoomTween.TweenProperty(this, PropertyName.Zoom.ToString(), new Vector2(zoomAmount, zoomAmount), duration);
        GD.Print($"ShakeyCamera.ZoomToPoint: Property bound. Tween should start playing implicitly.");

        // Optional: Add a finished signal handler for debugging
        zoomTween.Finished += () => GD.Print("ShakeyCamera.ZoomToPoint: Zoom Tween Finished.");

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

    private void ResetShake()
    {
        shakeStrength = 0.0f;
        shakeDuration = 0.0f;
        Offset = Vector2.Zero;
        Rotation = 0.0f;
        // GD.Print("ShakeyCamera: Shake reset."); // Optional debug
    }

    public void ResetZoom()
    {
        zoomTween?.Kill();
        Zoom = Vector2.One;
        GD.Print("ShakeyCamera.ResetZoom: Zoom reset to 1.0.");
    }
}