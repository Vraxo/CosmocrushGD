﻿using Godot;

namespace CosmocrushGD;

public partial class UIButton : Button
{
    [Export] public bool TweenScale { get; set; } = false;

    private Tween activeTween;
    private Vector2 originalScale;

    public override void _Ready()
    {
        originalScale = Scale;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Pressed += OnButtonPressed;
        Resized += OnResized;

        UpdatePivot();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(this))
        {
            MouseEntered -= OnMouseEntered;
            MouseExited -= OnMouseExited;
            Pressed -= OnButtonPressed;
            Resized -= OnResized;
        }
        base._ExitTree();
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
        if (!TweenScale)
        {
            return;
        }

        activeTween?.Kill();
        AnimateHover(1.5f);
    }

    private void OnMouseExited()
    {
        if (!TweenScale)
        {
            return;
        }

        activeTween?.Kill();
        AnimateHover(1.0f);
    }

    private void AnimateHover(float targetScale)
    {
        if (!TweenScale)
        {
            return;
        }

        activeTween = CreateTween()
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        activeTween.TweenProperty(
            this,
            PropertyName.Scale.ToString(),
            originalScale * targetScale,
            0.2f);
    }

    private void OnButtonPressed()
    {
        GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
    }
}