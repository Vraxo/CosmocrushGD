using Godot;
using System;

public partial class TransitionScreen : CanvasLayer
{
    [Signal]
    public delegate void TransitionFinishedEventHandler();

    private ColorRect _colorRect;
    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _colorRect = GetNode<ColorRect>("ColorRect");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        SetupAnimations();

        // Explicitly connect the animation_finished signal
        _animationPlayer.AnimationFinished += _on_AnimationPlayer_animation_finished;

        // Start with the screen invisible and non-blocking
        _colorRect.Visible = false;
        // Ensure the ColorRect doesn't block mouse input when invisible
        _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore; 
    }

    public async void FadeTransition()
    {
        GD.Print("TransitionScreen: FadeTransition() called.");
        // Make the screen visible and block input during transition
        _colorRect.Visible = true;
        _colorRect.MouseFilter = Control.MouseFilterEnum.Stop;

        _animationPlayer.Play("FadeToBlack");
        // Wait for FadeToBlack to finish (handled by the signal connection)
    }

    // This method should be connected to the AnimationPlayer's "animation_finished" signal in the Godot editor
    private void _on_AnimationPlayer_animation_finished(StringName animName)
    {
        GD.Print($"TransitionScreen: _on_AnimationPlayer_animation_finished called with animName = {animName}");
        if (animName == "FadeToBlack")
        {
            // Once faded to black, start fading back to normal
            _animationPlayer.Play("FadeToNormal");
            // The scene change should happen *during* the black screen,
            // so we emit the signal here.
            GD.Print("TransitionScreen: Emitting TransitionFinished signal.");
            EmitSignal(SignalName.TransitionFinished);
        }
        else if (animName == "FadeToNormal")
        {
            // Once faded back to normal, hide the screen and unblock input
            _colorRect.Visible = false;
            _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    public override void _ExitTree()
    {
        if (_animationPlayer != null)
        {
            _animationPlayer.AnimationFinished -= _on_AnimationPlayer_animation_finished;
        }
    }
}

    private void SetupAnimations()
    {
        if (_animationPlayer == null)
        {
            GD.PrintErr("TransitionScreen: Cannot setup animations, _animationPlayer is null!");
            return;
        }

        var animLib = new AnimationLibrary();

        // --- Create FadeToBlack Animation ---
        var fadeToBlackAnim = new Animation();
        fadeToBlackAnim.Length = 0.5f;
        // Add track for ColorRect modulate property
        int trackIdx = fadeToBlackAnim.AddTrack(Animation.TrackType.Value);
        fadeToBlackAnim.TrackSetPath(trackIdx, "ColorRect:modulate");
        // Add keyframes
        fadeToBlackAnim.TrackInsertKey(trackIdx, 0.0f, Colors.Black with { A = 0 }); // Start transparent black
        fadeToBlackAnim.TrackInsertKey(trackIdx, 0.5f, Colors.Black with { A = 1 }); // End opaque black

        animLib.AddAnimation("FadeToBlack", fadeToBlackAnim);


        // --- Create FadeToNormal Animation ---
        var fadeToNormalAnim = new Animation();
        fadeToNormalAnim.Length = 0.5f;
        // Add track for ColorRect modulate property
        trackIdx = fadeToNormalAnim.AddTrack(Animation.TrackType.Value);
        fadeToNormalAnim.TrackSetPath(trackIdx, "ColorRect:modulate");
        // Add keyframes
        fadeToNormalAnim.TrackInsertKey(trackIdx, 0.0f, Colors.Black with { A = 1 }); // Start opaque black
        fadeToNormalAnim.TrackInsertKey(trackIdx, 0.5f, Colors.Black with { A = 0 }); // End transparent black

        animLib.AddAnimation("FadeToNormal", fadeToNormalAnim);


        // --- Add library to AnimationPlayer ---
        // Check if default library "" exists, remove if it does to avoid conflicts
        if (_animationPlayer.HasAnimationLibrary(""))
        {
             _animationPlayer.RemoveAnimationLibrary("");
        }
        _animationPlayer.AddAnimationLibrary("", animLib); // Add as the default library

        GD.Print("TransitionScreen: Animations created and added programmatically.");
    }
}
