using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class SceneTransitionManager : CanvasLayer
{
    [Export] private ColorRect fadeRect;
    [Export] private double fadeDuration = 0.8;

    public static SceneTransitionManager Instance { get; private set; }

    private bool isTransitioning = false;
    private Tween activeTween;
    private string currentScenePath = "";

    public override void _EnterTree()
    {
        if (Instance is null)
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always;
            if (GetTree().CurrentScene is not null)
            {
                currentScenePath = GetTree().CurrentScene.SceneFilePath;
            }
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
        fadeRect.Visible = false;
    }

    public async void ChangeScene(string scenePath)
    {
        if (fadeRect is null || isTransitioning || scenePath == currentScenePath)
        {
            var reason = fadeRect is null
                ? "FadeRect null"
                : isTransitioning
                    ? "Transition in progress"
                    : "Same scene requested";
            GD.Print($"SceneTransitionManager: Transition to {scenePath} blocked ({reason}). Current: {currentScenePath}");
            return;
        }

        isTransitioning = true;
        GD.Print($"SceneTransitionManager: Starting transition from {currentScenePath} to {scenePath}");

        activeTween?.Kill();
        fadeRect.Modulate = Colors.Transparent;
        fadeRect.Visible = true;
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 1.0f, fadeDuration);

        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade out finished.");

        CleanUpPools();

        var loadTask = LoadSceneAsync(scenePath);
        var loadedScene = await loadTask;

        if (loadedScene is null)
        {
            GD.PrintErr($"SceneTransitionManager: Failed to load scene {scenePath} asynchronously.");
            ResetFade();
            isTransitioning = false;
            return;
        }
        GD.Print("SceneTransitionManager: Scene loaded asynchronously.");

        var currentScene = GetTree().CurrentScene;
        if (currentScene is not null)
        {
            if (IsInstanceValid(currentScene))
            {
                GetTree().CurrentScene = null;
                currentScene.QueueFree();
                GD.Print($"SceneTransitionManager: Queued freeing of previous scene: {currentScenePath}");
            }
            else
            {
                GD.Print($"SceneTransitionManager: Previous scene instance ({currentScenePath}) was already invalid before QueueFree.");
            }
        }

        var newSceneInstance = loadedScene.Instantiate();
        GetTree().Root.AddChild(newSceneInstance);
        GetTree().CurrentScene = newSceneInstance;
        currentScenePath = scenePath;
        GD.Print($"SceneTransitionManager: Instantiated and set current scene to {scenePath}");

        if (GetTree().Paused)
        {
            GetTree().Paused = false;
            GD.Print("SceneTransitionManager: Unpaused tree for new scene.");
        }

        activeTween?.Kill();
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 0.0f, fadeDuration);
        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade in finished.");

        fadeRect.Visible = false;
        isTransitioning = false;
    }

    private void CleanUpPools()
    {
        GD.Print("SceneTransitionManager: Cleaning up (No pools to clean now)...");
    }

    private async Task<PackedScene> LoadSceneAsync(string path)
    {
        ResourceLoader.LoadThreadedRequest(path);
        GD.Print($"SceneTransitionManager: Started threaded load for {path}");

        while (true)
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path);
            if (status == ResourceLoader.ThreadLoadStatus.InProgress)
            {
                await Task.Delay(16);
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                GD.Print($"SceneTransitionManager: Threaded load finished for {path}");
                return ResourceLoader.LoadThreadedGet(path) as PackedScene;
            }
            else
            {
                GD.PrintErr($"SceneTransitionManager: Threaded load failed for {path}. Status: {status}");
                return null;
            }
        }
    }

    private void ResetFade()
    {
        activeTween?.Kill();
        if (fadeRect is not null)
        {
            fadeRect.Modulate = Colors.Transparent;
            fadeRect.Visible = false;
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