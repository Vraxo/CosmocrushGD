using Godot;

namespace CosmocrushGD;

public partial class UIButton : Button
{
    private Tween activeTween;
    private Vector2 originalScale;

    public override void _Ready()
    {
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Resized += OnResized; // Update pivot when size changes

        UpdatePivot();
        originalScale = Scale;
    }

    private void OnResized()
    {
        UpdatePivot();
    }

    private void UpdatePivot()
    {
        // Use RectSize instead of Size for accurate dimensions
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

        activeTween.TweenProperty(this, "scale", originalScale * targetScale, 0.2f);
    }
}