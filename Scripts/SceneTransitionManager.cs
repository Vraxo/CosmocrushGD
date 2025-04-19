using Godot;
using System.Diagnostics;
using System.Resources;
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
            else
            {
                currentScenePath = "";
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
            return;
        }

        fadeRect.Modulate = Colors.Transparent;
        fadeRect.Visible = true;
    }

    public async void ChangeScene(string scenePath)
    {

        if (fadeRect is null || isTransitioning)
        {
            return;
        }

        if (scenePath == currentScenePath)
        {
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


        var loadTask = LoadSceneAsync(scenePath);


        await ToSignal(activeTween, Tween.SignalName.Finished);


        PackedScene loadedScene = await loadTask;
        if (loadedScene is null)
        {
            ResetFade();
            isTransitioning = false;
            return;
        }


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

    private async Task<PackedScene> LoadSceneAsync(string path)
    {
        ResourceLoader.LoadThreadedRequest(path);
        while (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            await Task.Delay(16);
        }

        if (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            return ResourceLoader.LoadThreadedGet(path) as PackedScene;
        }
        else
        {
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