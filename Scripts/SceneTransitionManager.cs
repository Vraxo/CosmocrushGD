using Godot;
using System.Threading.Tasks; // Keep using Task for async/await convenience

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
            currentScenePath = GetTree().CurrentScene.SceneFilePath;
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
        if (fadeRect is null || isTransitioning || scenePath == currentScenePath)
        {
            GD.Print($"SceneTransitionManager: Transition blocked (In progress: {isTransitioning}, FadeRect null: {fadeRect is null}, Same scene: {scenePath == currentScenePath}).");
            return;
        }

        isTransitioning = true;
        GD.Print($"SceneTransitionManager: Starting transition to {scenePath}");


        activeTween?.Kill();
        fadeRect.Visible = true;
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 1.0f, fadeDuration);


        var loadTask = LoadSceneAsync(scenePath);


        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade out finished.");


        PackedScene loadedScene = await loadTask;
        if (loadedScene is null)
        {
            GD.PrintErr($"SceneTransitionManager: Failed to load scene {scenePath} asynchronously.");
            ResetFade();
            isTransitioning = false;
            return;
        }
        GD.Print("SceneTransitionManager: Scene loaded asynchronously.");


        if (GetTree().Paused)
        {
            GetTree().Paused = false;
        }


        Node currentScene = GetTree().CurrentScene;
        GetTree().CurrentScene = null;
        currentScene?.QueueFree();


        Node newSceneInstance = loadedScene.Instantiate();
        GetTree().Root.AddChild(newSceneInstance);
        GetTree().CurrentScene = newSceneInstance;
        currentScenePath = scenePath;
        GD.Print($"SceneTransitionManager: Instantiated and set current scene to {scenePath}");


        activeTween?.Kill();
        activeTween = CreateTween();
        activeTween.SetParallel(false);
        activeTween.SetProcessMode(Tween.TweenProcessMode.Idle);
        activeTween.SetEase(Tween.EaseType.InOut);
        activeTween.SetTrans(Tween.TransitionType.Linear);
        activeTween.TweenProperty(fadeRect, "modulate:a", 0.0f, fadeDuration);
        await ToSignal(activeTween, Tween.SignalName.Finished);
        GD.Print("SceneTransitionManager: Fade in finished.");

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