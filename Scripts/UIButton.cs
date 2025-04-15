using Godot;

namespace CosmocrushGD;

public partial class UIButton : Button
{
    private Tween activeTween;
    private Vector2 originalScale;

    public override void _Ready()
    {
        originalScale = Scale;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Resized += OnResized;

        UpdatePivot();
    }

    private void OnResized()
    {
        UpdatePivot();
    }

    private void UpdatePivot()
    {
        PivotOffset = Size / 2;
    }

    private void OnMouseEntered()
    {
        activeTween?.Stop();
        AnimateHover(1.5f);
    }

    private void OnMouseExited()
    {
        activeTween?.Stop();
        AnimateHover(1.0f);
    }

    private void AnimateHover(float targetScale)
    {
        activeTween = CreateTween()
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        activeTween.TweenProperty(
            this,
            "scale",
            originalScale * targetScale,
            0.2f);
    }
}