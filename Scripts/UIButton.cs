// Summary: Removed the incorrect KillTweensOf call. Kept the explicit killing of the locally tracked activeHoverTween in AnimateHover to handle rapid mouse enter/exit.
using Godot;

namespace CosmocrushGD;

public partial class UIButton : Button
{
    private Tween activeHoverTween;
    private Vector2 originalScale;

    public override void _Ready()
    {
        originalScale = Vector2.One;

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
        // No longer attempting to kill external tweens here.
        // Relying on the new tween creation in AnimateHover possibly overriding.
        AnimateHover(1.5f);
    }

    private void OnMouseExited()
    {
        // No longer attempting to kill external tweens here.
        AnimateHover(1.0f);
    }

    private void AnimateHover(float targetScale)
    {
        // Kill the specific hover tween *this script* might have created previously.
        // This prevents overlapping hover animations from rapid mouse movements.
        activeHoverTween?.Kill();

        // Create the new hover tween. This might implicitly stop
        // the initial scale tween from NewMainMenu if it's still running
        // on this button's scale property.
        activeHoverTween = CreateTween()
            .SetTrans(Tween.TransitionType.Back) // Reverted to Back as Expo was just for testing
            .SetEase(Tween.EaseType.Out);

        activeHoverTween.TweenProperty(
            this,
            "scale",
            originalScale * targetScale,
            0.2f);
    }

    public override void _ExitTree()
    {
        // Clean up signals connected in _Ready
        if (IsInstanceValid(this))
        {
            MouseEntered -= OnMouseEntered;
            MouseExited -= OnMouseExited;
            Resized -= OnResized;
        }
        // Ensure the active tween is killed if the button is removed
        activeHoverTween?.Kill();
        base._ExitTree();
    }
}