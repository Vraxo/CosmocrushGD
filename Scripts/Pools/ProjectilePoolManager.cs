using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class ProjectilePoolManager : Node
{
    private static ProjectilePoolManager _instance;
    public static ProjectilePoolManager Instance => _instance ??= new ProjectilePoolManager();

    private PackedScene defaultProjectileScene;

    private int targetProjectilePoolSize = 80;

    private readonly Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();
    private bool poolsInitialized = false;
    private bool initializationStarted = false;

    public override void _EnterTree()
    {
        if (_instance is not null)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        base._ExitTree();
    }

    public async Task InitializePoolsAsync()
    {
        if (poolsInitialized || initializationStarted)
        {
            GD.Print($"ProjectilePoolManager: Initialization skipped (Initialized: {poolsInitialized}, Started: {initializationStarted})");
            return;
        }
        initializationStarted = true;
        GD.Print("ProjectilePoolManager: Starting initialization...");

        defaultProjectileScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");

        if (defaultProjectileScene is null)
        {
            GD.PrintErr("ProjectilePoolManager: Failed to load default Projectile.tscn");
        }

        await InitializeSinglePoolAsync(defaultProjectileScene, targetProjectilePoolSize, "Default Projectiles");

        poolsInitialized = true;
        initializationStarted = false;
        GD.Print("ProjectilePoolManager: Initialization complete.");
    }

    private async Task InitializeSinglePoolAsync(PackedScene scene, int targetSize, string poolName)
    {
        if (scene is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Cannot initialize pool '{poolName}': Scene is null.");
            return;
        }

        if (!availableProjectiles.TryGetValue(scene, out var queue))
        {
            queue = new Queue<Projectile>(targetSize);
            availableProjectiles.Add(scene, queue);
            GD.Print($"ProjectilePoolManager: Created new queue for '{poolName}'.");
        }
        else
        {
            GD.Print($"ProjectilePoolManager: Pool '{poolName}' for scene {scene.ResourcePath} already existed. Ensuring target size.");
        }

        int needed = targetSize - queue.Count;
        int createdCount = 0;
        GD.Print($"ProjectilePoolManager: Pool '{poolName}' needs {needed} instances.");

        for (int i = 0; i < needed; i++)
        {
            Projectile instance = CreateAndSetupProjectile(scene);
            if (instance is not null)
            {
                queue.Enqueue(instance);
                createdCount++;
            }
            await Task.Yield();
        }
        GD.Print($"ProjectilePoolManager: - {poolName} Pool ({scene.ResourcePath}): {queue.Count}/{targetSize} (Added {createdCount})");
    }

    private Projectile CreateAndSetupProjectile(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("ProjectilePoolManager: Cannot create projectile: scene is null.");
            return null;
        }

        var projectile = scene.Instantiate<Projectile>();
        if (projectile is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Failed to instantiate projectile from scene: {scene.ResourcePath}");
            return null;
        }

        projectile.SourceScene = scene;
        projectile.TopLevel = true;
        projectile.Visible = false;
        projectile.ProcessMode = ProcessModeEnum.Disabled;
        projectile.ZIndex = Projectile.ProjectileZIndex;
        AddChild(projectile);
        return projectile;
    }

    public Projectile GetProjectile(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("ProjectilePoolManager.GetProjectile: Null scene provided.");
            return null;
        }

        if (!poolsInitialized)
        {
            GD.PushWarning("ProjectilePoolManager.GetProjectile called before pools fully initialized!");
            var emergencyProjectile = CreateAndSetupProjectile(scene);
            if (emergencyProjectile is not null)
            {
                GD.PrintErr("ProjectilePoolManager: Returning emergency projectile instance.");
                emergencyProjectile.Visible = false;
                emergencyProjectile.ProcessMode = ProcessModeEnum.Disabled;
            }
            return emergencyProjectile;
        }

        if (!availableProjectiles.TryGetValue(scene, out var queue))
        {
            GD.PrintErr($"ProjectilePoolManager.GetProjectile: Pool not found for {scene.ResourcePath}! Was it initialized? Creating fallback instance.");
            var fallbackProjectile = CreateAndSetupProjectile(scene);
            if (fallbackProjectile is not null)
            {
                fallbackProjectile.Visible = false;
                fallbackProjectile.ProcessMode = ProcessModeEnum.Disabled;
            }
            return fallbackProjectile;
        }

        Projectile projectile = null;
        while (queue.Count > 0)
        {
            projectile = queue.Dequeue();
            if (projectile is not null && IsInstanceValid(projectile))
            {
                break;
            }
            GD.PrintErr($"ProjectilePoolManager: Discarded invalid projectile from pool {scene.ResourcePath}.");
            projectile = null;
        }

        if (projectile is null)
        {
            GD.Print($"ProjectilePoolManager: Pool empty or contained only invalid instances for {scene.ResourcePath}! Creating new instance.");
            projectile = CreateAndSetupProjectile(scene);
            if (projectile is null)
            {
                return null;
            }
        }

        projectile.Visible = false;
        projectile.ProcessMode = ProcessModeEnum.Disabled;

        return projectile;
    }

    public void ReturnProjectileToPool(Projectile projectile)
    {
        if (projectile is null || !IsInstanceValid(projectile))
        {
            GD.PrintErr($"ProjectilePoolManager.ReturnProjectileToPool: Invalid projectile instance {projectile?.GetInstanceId()}.");
            return;
        }
        if (projectile.SourceScene is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Projectile {projectile.GetInstanceId()} cannot return to pool: SourceScene is null. Freeing.");
            projectile.QueueFree();
            return;
        }

        if (!availableProjectiles.TryGetValue(projectile.SourceScene, out var queue))
        {
            GD.PrintErr($"ProjectilePoolManager: Pool not found for {projectile.SourceScene.ResourcePath} on return. Freeing projectile {projectile.GetInstanceId()}.");
            projectile.QueueFree();
            return;
        }

        projectile.ResetForPooling();
        queue.Enqueue(projectile);
    }

    public void CleanUpActiveObjects()
    {
        GD.Print("ProjectilePoolManager: Cleaning up active projectiles...");
        var nodesToClean = new List<Projectile>();

        // Iterate children safely in case ReturnProjectileToPool modifies the collection
        foreach (Node child in GetChildren().OfType<Projectile>())
        {
            if (child is Projectile projectile && IsInstanceValid(projectile) && projectile.ProcessMode != ProcessModeEnum.Disabled)
            {
                nodesToClean.Add(projectile);
            }
        }


        GD.Print($"ProjectilePoolManager: Found {nodesToClean.Count} active projectiles to clean.");

        foreach (var projectile in nodesToClean)
        {
            if (IsInstanceValid(projectile))
            {
                GD.Print($" - Returning active projectile {projectile.GetInstanceId()} to pool.");
                ReturnProjectileToPool(projectile);
            }
        }
        GD.Print("ProjectilePoolManager: Finished cleaning active projectiles.");
    }
}