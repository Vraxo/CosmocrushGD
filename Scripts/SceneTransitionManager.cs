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
            string reason = fadeRect is null
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
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle); // Use Idle to run even if game is paused
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 1.0f, fadeDuration);


        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade out finished.");

        // --- Clean up active objects BEFORE freeing the scene ---
        CleanUpPools();
        // --------------------------------------------------------

        var loadTask = LoadSceneAsync(scenePath);
        PackedScene loadedScene = await loadTask;

        if (loadedScene is null)
        {
            GD.PrintErr($"SceneTransitionManager: Failed to load scene {scenePath} asynchronously.");
            ResetFade(); // Ensure fade is reset on failure
            isTransitioning = false;
            return;
        }
        GD.Print("SceneTransitionManager: Scene loaded asynchronously.");


        Node currentScene = GetTree().CurrentScene;
        if (currentScene is not null)
        {
            // Prevent potential errors if scene is already being freed elsewhere
            if (IsInstanceValid(currentScene))
            {
                GetTree().CurrentScene = null; // Dereference before freeing
                currentScene.QueueFree();
                GD.Print($"SceneTransitionManager: Queued freeing of previous scene: {currentScenePath}");
            }
            else
            {
                GD.Print($"SceneTransitionManager: Previous scene instance ({currentScenePath}) was already invalid before QueueFree.");
            }
        }


        Node newSceneInstance = loadedScene.Instantiate();
        GetTree().Root.AddChild(newSceneInstance);
        GetTree().CurrentScene = newSceneInstance; // Set the new scene as current
        currentScenePath = scenePath; // Update the current path tracker
        GD.Print($"SceneTransitionManager: Instantiated and set current scene to {scenePath}");


        if (GetTree().Paused)
        {
            GetTree().Paused = false; // Ensure the new scene starts unpaused
            GD.Print("SceneTransitionManager: Unpaused tree for new scene.");
        }


        // Fade In
        activeTween?.Kill();
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 0.0f, fadeDuration);
        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade in finished.");

        fadeRect.Visible = false; // Hide rect after fade in
        isTransitioning = false;
    }

    private void CleanUpPools()
    {
        GD.Print("SceneTransitionManager: Cleaning up pools before scene change...");

        // Call cleanup on each pool manager instance
        ParticlePoolManager.Instance?.CleanUpActiveObjects();
        DamageIndicatorPoolManager.Instance?.CleanUpActiveObjects();
        ProjectilePoolManager.Instance?.CleanUpActiveObjects();
        // GlobalAudioPlayer doesn't currently have an explicit cleanup for active sounds,
        // as they return themselves to the pool on 'Finished'. If needed, add one.
        // GlobalAudioPlayer.Instance?.CleanUpActiveAudioPlayers();

        GD.Print("SceneTransitionManager: Finished cleaning pools.");
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
                await Task.Delay(16); // Wait a short time (approx 1 frame)
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
        activeTween?.Kill(); // Stop any ongoing tween
        if (fadeRect is not null)
        {
            fadeRect.Modulate = Colors.Transparent; // Reset alpha
            fadeRect.Visible = false; // Hide the rect
        }
    }

    public override void _ExitTree()
    {
        activeTween?.Kill(); // Ensure tween is killed if manager is removed
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }
}