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
    private string currentScenePath = "";
    private const string GameScenePath = "res://Scenes/World.tscn";


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
            GD.Print($"SceneTransitionManager: Transition blocked (In progress: {isTransitioning}, FadeRect null: {fadeRect is null}, Same scene: {scenePath == currentScenePath}).");
            return;
        }

        isTransitioning = true;
        GD.Print($"SceneTransitionManager: Starting transition to {scenePath}");


        activeTween?.Kill();
        fadeRect.Modulate = Colors.Transparent;
        fadeRect.Visible = true;
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        // Corrected EaseType: Use InOut for consistent behavior (though effect is minimal with Linear trans)
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear); // Keep Linear transition for constant speed fade
        activeTween.TweenProperty(fadeRect, "modulate:a", 1.0f, fadeDuration); // Fade Out


        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade out finished.");


        // --- Perform Heavy Initialization During Black Screen ---
        if (scenePath == GameScenePath && GlobalAudioPlayer.Instance is not null)
        {
            GD.Print("SceneTransitionManager: Initializing GlobalAudioPlayer gameplay pools...");
            await GlobalAudioPlayer.Instance.InitializeGameplayPoolsAsync();
            GD.Print("SceneTransitionManager: GlobalAudioPlayer gameplay pools initialized.");
        }
        else
        {
            GD.Print($"SceneTransitionManager: Skipping gameplay pool initialization for scene: {scenePath}");
        }


        // --- Load Scene Asynchronously ---
        var loadTask = LoadSceneAsync(scenePath);
        PackedScene loadedScene = await loadTask;

        if (loadedScene is null)
        {
            GD.PrintErr($"SceneTransitionManager: Failed to load scene {scenePath} asynchronously.");
            ResetFade();
            isTransitioning = false;
            return;
        }
        GD.Print("SceneTransitionManager: Scene loaded asynchronously.");


        // --- Swap Scenes ---
        if (GetTree().Paused)
        {
            GetTree().Paused = false;
            GD.Print("SceneTransitionManager: Unpaused tree.");
        }

        Node currentScene = GetTree().CurrentScene;
        GetTree().CurrentScene = null;
        currentScene?.QueueFree();


        Node newSceneInstance = loadedScene.Instantiate();
        GetTree().Root.AddChild(newSceneInstance);
        GetTree().CurrentScene = newSceneInstance;
        currentScenePath = scenePath;
        GD.Print($"SceneTransitionManager: Instantiated and set current scene to {scenePath}");


        // --- Fade In ---
        activeTween?.Kill();
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        // Corrected EaseType: Use InOut for consistent behavior
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear); // Keep Linear transition
        activeTween.TweenProperty(fadeRect, "modulate:a", 0.0f, fadeDuration); // Fade In
        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade in finished.");

        fadeRect.Visible = false;
        isTransitioning = false;
    }

    private async Task<PackedScene> LoadSceneAsync(string path)
    {
        ResourceLoader.LoadThreadedRequest(path);
        GD.Print($"SceneTransitionManager: Started threaded load for {path}");

        while (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            await Task.Delay(16);
        }

        if (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            GD.Print($"SceneTransitionManager: Threaded load finished for {path}");
            return ResourceLoader.LoadThreadedGet(path) as PackedScene;
        }
        else
        {
            GD.PrintErr($"SceneTransitionManager: Threaded load failed for {path}. Status: {ResourceLoader.LoadThreadedGetStatus(path)}");
            return null;
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