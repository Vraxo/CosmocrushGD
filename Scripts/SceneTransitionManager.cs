using Godot;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class SceneTransitionManager : CanvasLayer
{
    [Export] private ColorRect fadeRect;
    [Export] private double fadeDuration = 0.8;

    public static SceneTransitionManager Instance { get; private set; }

    private bool isTransitioning = false;
    private Tween activeTween;

    public override void _EnterTree()
    {
        if (Instance is null)
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always;
        }
        else
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        if (fadeRect is null)
        {
            GD.PrintErr("SceneTransitionManager: FadeRect node not assigned!");
            return;
        }

        fadeRect.Modulate = Colors.Transparent;
        fadeRect.Visible = true;
    }

    public async void ChangeScene(string scenePath)
    {
        if (fadeRect is null || isTransitioning)
        {
            GD.Print("SceneTransitionManager: Transition already in progress or FadeRect is null.");
            return;
        }

        isTransitioning = true;
        activeTween?.Kill();
        fadeRect.Visible = true;

        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);

        activeTween.TweenProperty(fadeRect, "modulate:a", 1.0f, fadeDuration);
        await ToSignal(activeTween, Tween.SignalName.Finished);

        if (GetTree().Paused)
        {
            GetTree().Paused = false;
        }

        var error = GetTree().ChangeSceneToFile(scenePath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"SceneTransitionManager: Failed to change scene to {scenePath}. Error: {error}");
            ResetFade();
            isTransitioning = false;
            return;
        }

        activeTween?.Kill();
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);

        activeTween.TweenProperty(fadeRect, "modulate:a", 0.0f, fadeDuration);
        await ToSignal(activeTween, Tween.SignalName.Finished);

        isTransitioning = false;
    }


    private void ResetFade()
    {
        activeTween?.Kill();
        if (fadeRect is not null)
        {
            fadeRect.Modulate = Colors.Transparent;
            fadeRect.Visible = true;
        }
    }

    public override void _ExitTree()
    {
        activeTween?.Kill();

        if (Instance == this)
        {
            Instance = null;
        }

        base._ExitTree();
    }
}