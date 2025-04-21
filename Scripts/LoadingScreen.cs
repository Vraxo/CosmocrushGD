using Godot;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class LoadingScreen : Control
{
    [Export] private string menuShellScenePath = "res://Scenes/Menu/MenuShell.tscn";

    private PackedScene _loadedMenuShellScene;

    public override void _Ready()
    {
        // Ensure label is visible for at least one frame before starting load
        CallDeferred(nameof(StartLoading));
    }

    private async void StartLoading()
    {
        GD.Print("LoadingScreen: Starting background loading...");

        // Task 1: Initialize Global Audio Player Pools (if it exists)
        Task audioInitTask = Task.CompletedTask; // Default to completed task
        if (GlobalAudioPlayer.Instance is not null)
        {
            audioInitTask = GlobalAudioPlayer.Instance.InitializeGameplayPoolsAsync();
        }
        else
        {
            GD.PrintErr("LoadingScreen: GlobalAudioPlayer instance not found during loading!");
        }

        // Task 2: Load the Main Menu Scene Resource (can run concurrently)
        Task<PackedScene> menuLoadTask = LoadSceneAsync(menuShellScenePath);

        // Wait for both tasks to complete
        await Task.WhenAll(audioInitTask, menuLoadTask);

        _loadedMenuShellScene = menuLoadTask.Result; // Get result from completed task

        if (_loadedMenuShellScene is null)
        {
            GD.PrintErr($"LoadingScreen: Failed to load MenuShell scene ({menuShellScenePath}). Cannot continue.");
            // Handle error appropriately, maybe show an error message or quit
            // GetTree().Quit();
            return;
        }

        GD.Print("LoadingScreen: Loading and initialization complete. Changing scene...");

        // Directly change scene - no fancy transition needed from loading screen
        Error err = GetTree().ChangeSceneToPacked(_loadedMenuShellScene);
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

        while (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            // Wait a frame to allow other processing and loading thread
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        if (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            GD.Print($"LoadingScreen: Threaded load finished for {path}");
            return ResourceLoader.LoadThreadedGet(path) as PackedScene;
        }
        else
        {
            GD.PrintErr($"LoadingScreen: Threaded load failed for {path}. Status: {ResourceLoader.LoadThreadedGetStatus(path)}");
            return null;
        }
    }
}