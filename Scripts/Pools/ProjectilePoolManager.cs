using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class ProjectilePoolManager : Node
{
    public static ProjectilePoolManager Instance { get; private set; }

    private PackedScene defaultProjectileScene;
    // Add other projectile scenes here if needed
    // private PackedScene laserProjectileScene;

    private int targetProjectilePoolSize = 60;
    // Add other pool sizes if needed
    // private int targetLaserPoolSize = 30;

    private Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();
    private bool poolsInitialized = false;
    private bool initializationStarted = false;

    public override void _EnterTree()
    {
        if (Instance is not null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
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

        // Load all projectile scenes needed
        defaultProjectileScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");
        // laserProjectileScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/LaserProjectile.tscn"); // Example

        if (defaultProjectileScene is null) GD.PrintErr("ProjectilePoolManager: Failed to load default Projectile.tscn");
        // if (laserProjectileScene is null) GD.PrintErr("ProjectilePoolManager: Failed to load LaserProjectile.tscn"); // Example

        // Initialize all required pools
        await InitializeSinglePoolAsync(defaultProjectileScene, targetProjectilePoolSize, "Default Projectiles");
        // await InitializeSinglePoolAsync(laserProjectileScene, targetLaserPoolSize, "Laser Projectiles"); // Example

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
            await Task.Yield(); // Allow engine processing
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
        projectile.SourceScene = scene; // Important for returning to the correct pool
        projectile.TopLevel = true; // Independent of parent transform
        projectile.Visible = false; // Start invisible
        projectile.ProcessMode = ProcessModeEnum.Disabled; // Start disabled
        projectile.ZIndex = Projectile.ProjectileZIndex; // Use constant from Projectile script
        AddChild(projectile); // Add to the manager node
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
                // Needs minimal setup, caller will use .Setup() and .Activate()
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
                // Minimal setup, caller handles the rest via .Setup()/.Activate()
                fallbackProjectile.Visible = false;
                fallbackProjectile.ProcessMode = ProcessModeEnum.Disabled;
            }
            return fallbackProjectile;
        }

        Projectile projectile;
        if (queue.Count > 0)
        {
            projectile = queue.Dequeue();
            if (projectile is null || !IsInstanceValid(projectile))
            {
                GD.PrintErr($"ProjectilePoolManager: Invalid projectile found in pool for {scene.ResourcePath}. Creating replacement.");
                projectile = CreateAndSetupProjectile(scene);
                if (projectile is null) return null;
            }
        }
        else
        {
            GD.Print($"ProjectilePoolManager: Pool empty for {scene.ResourcePath}! Creating new instance.");
            projectile = CreateAndSetupProjectile(scene);
            if (projectile is null) return null;
        }

        // Basic state reset before returning to caller.
        // Caller is responsible for calling projectile.Setup() and projectile.Activate().
        projectile.Visible = false;
        projectile.ProcessMode = ProcessModeEnum.Disabled;
        // ZIndex set during creation

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

        projectile.ResetForPooling(); // Use the projectile's own reset method
        queue.Enqueue(projectile);
    }

    public void CleanUpActiveObjects()
    {
        GD.Print("ProjectilePoolManager: Cleaning up active projectiles...");
        var nodesToClean = new List<Projectile>();

        foreach (Node child in GetChildren())
        {
            if (child is Projectile projectile && projectile.ProcessMode != ProcessModeEnum.Disabled)
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