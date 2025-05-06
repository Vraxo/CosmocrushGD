using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CosmocrushGD;

public partial class ProjectilePoolManager : Node
{
    private static ProjectilePoolManager _instance;
    public static ProjectilePoolManager Instance => _instance ??= new ProjectilePoolManager();

    private struct ProjectileActivationRequest
    {
        public Projectile ProjectileInstance;
        public Vector2 StartPosition;
        public Vector2 Direction;
        public Texture2D SpriteTexture;
        public Color? ParticleColor;
    }

    private const int MaxActivationsPerFrame = 1;

    private PackedScene defaultProjectileScene;
    private int targetProjectilePoolSize = 80;

    private readonly Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();
    private readonly Queue<ProjectileActivationRequest> _projectileActivationQueue = new();
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

    public override void _Process(double delta)
    {
        int activationsThisFrame = 0;
        while (_projectileActivationQueue.Count > 0 && activationsThisFrame < MaxActivationsPerFrame)
        {
            var request = _projectileActivationQueue.Dequeue();
            if (request.ProjectileInstance is not null && IsInstanceValid(request.ProjectileInstance))
            {
                request.ProjectileInstance.SetupAndActivate(
                    request.StartPosition,
                    request.Direction,
                    request.SpriteTexture,
                    request.ParticleColor
                );
                activationsThisFrame++;
            }
            else
            {
                GD.PrintErr($"ProjectilePoolManager: Tried to activate an invalid projectile instance from queue. ID: {request.ProjectileInstance?.GetInstanceId().ToString() ?? "null"}");
            }
        }
    }

    public async Task InitializePoolsAsync()
    {
        if (poolsInitialized || initializationStarted)
        {
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
        }

        int needed = targetSize - queue.Count;
        int createdCount = 0;
        GD.Print($"ProjectilePoolManager: Pool '{poolName}' (Target: {targetSize}) needs {needed} instances.");

        for (int i = 0; i < needed; i++)
        {
            Projectile instance = CreateAndSetupProjectile(scene);
            if (instance is not null)
            {
                queue.Enqueue(instance);
                createdCount++;
            }
            else
            {
                GD.PrintErr($"ProjectilePoolManager: Failed to create and setup projectile instance {i + 1}/{needed} for pool '{poolName}'.");
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

        var node = scene.Instantiate();
        if (node is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Instantiation returned null for scene: {scene.ResourcePath}");
            return null;
        }

        var projectile = node as Projectile;

        if (projectile is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Failed to cast instantiated node to Projectile. Node type was '{node.GetType().FullName}'. Scene: {scene.ResourcePath}. Check script attachment and build.");
            node.QueueFree();
            return null;
        }
        GD.Print($"ProjectilePoolManager: Created new projectile instance {projectile.GetInstanceId()} for scene {scene.ResourcePath}");
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
        GD.Print($"ProjectilePoolManager: GetProjectile called for scene {scene.ResourcePath}");

        if (!poolsInitialized)
        {
            GD.PushWarning("ProjectilePoolManager.GetProjectile called before pools fully initialized! Creating emergency instance.");
            var emergencyProjectile = CreateAndSetupProjectile(scene);
            if (emergencyProjectile is not null)
            {
                GD.Print($"ProjectilePoolManager: Returning emergency projectile {emergencyProjectile.GetInstanceId()}");
            }
            return emergencyProjectile;
        }

        if (!availableProjectiles.TryGetValue(scene, out var queue))
        {
            GD.PrintErr($"ProjectilePoolManager.GetProjectile: Pool not found for {scene.ResourcePath}! This should not happen if initialized. Creating fallback instance.");
            var fallbackProjectile = CreateAndSetupProjectile(scene);
            if (fallbackProjectile is not null)
            {
                GD.Print($"ProjectilePoolManager: Returning fallback projectile {fallbackProjectile.GetInstanceId()}");
            }
            return fallbackProjectile;
        }

        GD.Print($"ProjectilePoolManager: Queue size for {scene.ResourcePath} before dequeue: {queue.Count}");
        Projectile projectile = null;
        bool needsNewInstance = true;

        if (queue.Count > 0)
        {
            projectile = queue.Dequeue();
            if (projectile is not null && IsInstanceValid(projectile))
            {
                needsNewInstance = false;
                GD.Print($"ProjectilePoolManager: Got projectile {projectile.GetInstanceId()} from queue for {scene.ResourcePath}. Valid.");
            }
            else
            {
                GD.PrintErr($"ProjectilePoolManager: Dequeued invalid projectile from pool {scene.ResourcePath}. Instance ID: {projectile?.GetInstanceId().ToString() ?? "null"}. Will create new.");
                projectile = null;
            }
        }

        if (needsNewInstance)
        {
            if (projectile is null)
            {
                GD.Print($"ProjectilePoolManager: Pool empty for {scene.ResourcePath} or dequeued invalid. Creating new instance.");
            }
            projectile = CreateAndSetupProjectile(scene);
            if (projectile is null)
            {
                GD.PrintErr($"ProjectilePoolManager: Failed to create new instance for {scene.ResourcePath} after pool miss/invalid item.");
                return null;
            }
        }

        projectile.Visible = false;
        projectile.ProcessMode = ProcessModeEnum.Disabled;
        GD.Print($"ProjectilePoolManager: Returning projectile {projectile.GetInstanceId()} for {scene.ResourcePath} (for later activation). Queue size after: {queue.Count}");
        return projectile;
    }

    public void EnqueueProjectileActivation(Projectile projectile, Vector2 startPosition, Vector2 direction, Texture2D spriteTexture, Color? particleColor)
    {
        if (projectile is null || !IsInstanceValid(projectile))
        {
            GD.PrintErr("ProjectilePoolManager: Attempted to enqueue activation for an invalid projectile instance.");
            return;
        }

        var request = new ProjectileActivationRequest
        {
            ProjectileInstance = projectile,
            StartPosition = startPosition,
            Direction = direction,
            SpriteTexture = spriteTexture,
            ParticleColor = particleColor
        };
        _projectileActivationQueue.Enqueue(request);
        GD.Print($"ProjectilePoolManager: Enqueued activation for projectile {projectile.GetInstanceId()}. Queue size: {_projectileActivationQueue.Count}");
    }

    public void ReturnProjectileToPool(Projectile projectile)
    {
        if (projectile is null || !IsInstanceValid(projectile))
        {
            GD.PrintErr($"ProjectilePoolManager.ReturnProjectileToPool: Attempted to return invalid projectile instance {projectile?.GetInstanceId()}.");
            return;
        }
        if (projectile.SourceScene is null)
        {
            GD.PrintErr($"ProjectilePoolManager: Projectile {projectile.GetInstanceId()} cannot return to pool: SourceScene is null. Freeing.");
            projectile.QueueFree(); // Should not happen if pool flow is correct
            return;
        }

        GD.Print($"ProjectilePoolManager: Attempting to return projectile {projectile.GetInstanceId()} from scene {projectile.SourceScene.ResourcePath} to pool.");
        if (!availableProjectiles.TryGetValue(projectile.SourceScene, out var queue))
        {
            GD.PrintErr($"ProjectilePoolManager: Pool not found for {projectile.SourceScene.ResourcePath} on return. Freeing projectile {projectile.GetInstanceId()}.");
            projectile.QueueFree();
            return;
        }

        projectile.ResetForPooling();
        queue.Enqueue(projectile); // Re-enabled pooling
        GD.Print($"ProjectilePoolManager: Returned projectile {projectile.GetInstanceId()} to pool {projectile.SourceScene.ResourcePath}. New queue size: {queue.Count}");
    }


    public void CleanUpActiveObjects()
    {
        GD.Print("ProjectilePoolManager: Cleaning up active projectiles...");
        _projectileActivationQueue.Clear();
        GD.Print("ProjectilePoolManager: Cleared projectile activation queue.");

        var nodesToClean = new List<Projectile>();
        var allProjectilesInTree = new List<Projectile>();
        FindNodesOfType(GetTree().Root, allProjectilesInTree);

        foreach (var projectile in allProjectilesInTree)
        {
            if (IsInstanceValid(projectile) &&
                projectile.ProcessMode != ProcessModeEnum.Disabled &&
                projectile.SourceScene != null &&
                availableProjectiles.ContainsKey(projectile.SourceScene))
            {
                if (projectile.GetParent() != this)
                {
                    if (!nodesToClean.Contains(projectile))
                    {
                        nodesToClean.Add(projectile);
                    }
                }
                else if (!nodesToClean.Contains(projectile) && projectile.ProcessMode != ProcessModeEnum.Disabled)
                {
                    nodesToClean.Add(projectile);
                }
            }
        }

        GD.Print($"ProjectilePoolManager: Found {nodesToClean.Count} active projectiles potentially needing cleanup from tree scan.");

        foreach (var projectile in nodesToClean)
        {
            if (IsInstanceValid(projectile))
            {
                GD.Print($"ProjectilePoolManager: Cleaning up active projectile {projectile.GetInstanceId()} from scene. Returning to pool.");
                ReturnProjectileToPool(projectile);
            }
        }
        GD.Print("ProjectilePoolManager: Finished cleaning active projectiles.");
    }

    private void FindNodesOfType<T>(Node startNode, List<T> list) where T : Node
    {
        if (startNode is T nodeT)
        {
            list.Add(nodeT);
        }

        foreach (Node child in startNode.GetChildren())
        {
            FindNodesOfType(child, list);
        }
    }
}