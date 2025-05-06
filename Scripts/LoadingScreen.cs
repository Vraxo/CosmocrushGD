using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class LoadingScreen : Control
{
    [Export] private string menuShellScenePath = "res://Scenes/Menu/MenuShell.tscn";

    private PackedScene loadedMenuShellScene;

    public override void _Ready()
    {
        CallDeferred(nameof(StartLoading));
    }

    private async void StartLoading()
    {
        GD.Print("LoadingScreen: Starting background loading...");

        var initializationTasks = new List<Task>();

        // Task 1: Initialize Global Audio Player (Minimal) - Handled in its _Ready

        // Task 2: Initialize Particle Pools
        if (ParticlePoolManager.Instance is not null)
        {
            initializationTasks.Add(ParticlePoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: ParticlePoolManager instance not found!");
        }

        // Task 3: Initialize Damage Indicator Pools
        if (DamageIndicatorPoolManager.Instance is not null)
        {
            initializationTasks.Add(DamageIndicatorPoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: DamageIndicatorPoolManager instance not found!");
        }

        // Task 4: Initialize Projectile Pools
        if (ProjectilePoolManager.Instance is not null)
        {
            initializationTasks.Add(ProjectilePoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: ProjectilePoolManager instance not found!");
        }

        // Task 5: Initialize Enemy Pools
        if (EnemyPoolManager.Instance is not null)
        {
            initializationTasks.Add(EnemyPoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: EnemyPoolManager instance not found!");
        }

        // Task 6: Load the Main Menu Scene Resource
        Task<PackedScene> menuLoadTask = LoadSceneAsync(menuShellScenePath);
        initializationTasks.Add(menuLoadTask);

        GD.Print($"LoadingScreen: Awaiting {initializationTasks.Count} tasks...");
        await Task.WhenAll(initializationTasks);
        GD.Print("LoadingScreen: All loading/initialization tasks complete.");

        loadedMenuShellScene = await menuLoadTask; // Already awaited, just get result

        if (loadedMenuShellScene is null)
        {
            GD.PrintErr($"LoadingScreen: Failed to load MenuShell scene ({menuShellScenePath}). Cannot continue.");
            // GetTree().Quit(); // Optional: Quit on critical load failure
            return;
        }

        GD.Print("LoadingScreen: Changing scene...");

        Error err = GetTree().ChangeSceneToPacked(loadedMenuShellScene);
        if (err != Error.Ok)
        {
            GD.PrintErr($"LoadingScreen: Failed to change scene to MenuShell. Error: {err}");
        }
    }

    private async Task<PackedScene> LoadSceneAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            GD.PrintErr("LoadSceneAsync: Null or empty path provided.");
            return null;
        }

        ResourceLoader.LoadThreadedRequest(path);
        GD.Print($"LoadingScreen: Started threaded load for {path}");

        while (true)
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path);
            if (status == ResourceLoader.ThreadLoadStatus.InProgress)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                GD.Print($"LoadingScreen: Threaded load finished for {path}");
                var resource = ResourceLoader.LoadThreadedGet(path);
                return resource as PackedScene;
            }
            else
            {
                GD.PrintErr($"LoadingScreen: Threaded load failed or invalid status for {path}. Status: {status}");
                return null;
            }
        }
    }
}