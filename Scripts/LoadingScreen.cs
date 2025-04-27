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
        // Ensure label is visible for at least one frame before starting load
        CallDeferred(nameof(StartLoading));
    }

    private async void StartLoading()
    {
        GD.Print("LoadingScreen: Starting background loading...");

        // --- Tasks for Pool Initialization ---
        List<Task> initializationTasks = new();

        // Task 1: Initialize Global Audio Player (Minimal, already done in its _Ready essentially)
        // No async init needed for audio pool based on current GlobalAudioPlayer structure

        // Task 2: Initialize Particle Pools
        if (ParticlePoolManager.Instance is not null)
        {
            initializationTasks.Add(ParticlePoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: ParticlePoolManager instance not found during loading!");
        }

        // Task 3: Initialize Damage Indicator Pools
        if (DamageIndicatorPoolManager.Instance is not null)
        {
            initializationTasks.Add(DamageIndicatorPoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: DamageIndicatorPoolManager instance not found during loading!");
        }

        // Task 4: Initialize Projectile Pools
        if (ProjectilePoolManager.Instance is not null)
        {
            initializationTasks.Add(ProjectilePoolManager.Instance.InitializePoolsAsync());
        }
        else
        {
            GD.PrintErr("LoadingScreen: ProjectilePoolManager instance not found during loading!");
        }

        // Task 5: Load the Main Menu Scene Resource (can run concurrently with pool initializations)
        Task<PackedScene> menuLoadTask = LoadSceneAsync(menuShellScenePath);
        initializationTasks.Add(menuLoadTask); // Add scene loading to the list of tasks to await

        // Wait for all initialization and scene loading tasks to complete
        GD.Print($"LoadingScreen: Awaiting {initializationTasks.Count} tasks...");
        await Task.WhenAll(initializationTasks);
        GD.Print("LoadingScreen: All loading/initialization tasks complete.");

        // Retrieve the loaded scene result AFTER it has completed
        loadedMenuShellScene = menuLoadTask.Result;

        if (loadedMenuShellScene is null)
        {
            GD.PrintErr($"LoadingScreen: Failed to load MenuShell scene ({menuShellScenePath}). Cannot continue.");
            // Handle error appropriately, maybe show an error message or quit
            // GetTree().Quit();
            return;
        }

        GD.Print("LoadingScreen: Changing scene...");

        // Directly change scene - no fancy transition needed from loading screen
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

        // Check status in a loop
        while (true)
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path);
            if (status == ResourceLoader.ThreadLoadStatus.InProgress)
            {
                // Optional: Add progress reporting here if needed
                // var progress = ResourceLoader.LoadThreadedGetProgress(path);
                // GD.Print($"Loading {path}: {progress * 100:0.0}%");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); // Wait a frame
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                GD.Print($"LoadingScreen: Threaded load finished for {path}");
                return ResourceLoader.LoadThreadedGet(path) as PackedScene;
            }
            else // Failed or Invalid Status
            {
                GD.PrintErr($"LoadingScreen: Threaded load failed or invalid status for {path}. Status: {status}");
                return null;
            }
        }
    }
}