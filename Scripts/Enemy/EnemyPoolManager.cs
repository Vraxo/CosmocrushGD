using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
    public static EnemyPoolManager Instance { get; private set; }

    private PackedScene meleeEnemyScene;
    private PackedScene rangedEnemyScene;
    private PackedScene explodingEnemyScene;
    private PackedScene tankEnemyScene;
    private PackedScene swiftEnemyScene;

    private int targetPoolSizeMelee = 20;
    private int targetPoolSizeRanged = 15;
    private int targetPoolSizeExploding = 10;
    private int targetPoolSizeTank = 5;
    private int targetPoolSizeSwift = 15;

    private readonly Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
    private readonly Dictionary<PackedScene, int> targetPoolCounts = new();
    private bool poolsInitialized = false;
    private bool initializationStarted = false;

    private readonly List<EnemySpawner> _activeSpawners = new();

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
        _activeSpawners.Clear();
        base._ExitTree();
    }

    public void RegisterSpawner(EnemySpawner spawner)
    {
        if (spawner is not null && !_activeSpawners.Contains(spawner))
        {
            _activeSpawners.Add(spawner);
            GD.Print($"EnemyPoolManager: Registered spawner {spawner.Name}");
        }
    }

    public void UnregisterSpawner(EnemySpawner spawner)
    {
        if (spawner is not null && _activeSpawners.Remove(spawner))
        {
            GD.Print($"EnemyPoolManager: Unregistered spawner {spawner.Name}");
        }
    }

    public async Task InitializePoolsAsync()
    {
        if (poolsInitialized || initializationStarted)
        {
            GD.Print($"EnemyPoolManager: Initialization skipped (Initialized: {poolsInitialized}, Started: {initializationStarted})");
            return;
        }
        initializationStarted = true;
        GD.Print("EnemyPoolManager: Starting initialization...");

        LoadScenes();

        await InitializeSinglePoolAsync(meleeEnemyScene, targetPoolSizeMelee, "Melee Enemy");
        await InitializeSinglePoolAsync(rangedEnemyScene, targetPoolSizeRanged, "Ranged Enemy");
        await InitializeSinglePoolAsync(explodingEnemyScene, targetPoolSizeExploding, "Exploding Enemy");
        await InitializeSinglePoolAsync(tankEnemyScene, targetPoolSizeTank, "Tank Enemy");
        await InitializeSinglePoolAsync(swiftEnemyScene, targetPoolSizeSwift, "Swift Enemy");

        poolsInitialized = true;
        initializationStarted = false;
        GD.Print("EnemyPoolManager: Initialization complete.");
    }

    private void LoadScenes()
    {
        meleeEnemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/MeleeEnemy.tscn");
        rangedEnemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/RangedEnemy.tscn");
        explodingEnemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/ExplodingEnemy.tscn");
        tankEnemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/TankEnemy.tscn");
        swiftEnemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/SwiftEnemy.tscn");

        if (meleeEnemyScene is null) GD.PrintErr("EnemyPoolManager: Failed to load MeleeEnemy.tscn");
        if (rangedEnemyScene is null) GD.PrintErr("EnemyPoolManager: Failed to load RangedEnemy.tscn");
        if (explodingEnemyScene is null) GD.PrintErr("EnemyPoolManager: Failed to load ExplodingEnemy.tscn");
        if (tankEnemyScene is null) GD.PrintErr("EnemyPoolManager: Failed to load TankEnemy.tscn");
        if (swiftEnemyScene is null) GD.PrintErr("EnemyPoolManager: Failed to load SwiftEnemy.tscn");

        // Only add valid scenes to the counts dictionary
        if (meleeEnemyScene is not null) targetPoolCounts[meleeEnemyScene] = targetPoolSizeMelee;
        if (rangedEnemyScene is not null) targetPoolCounts[rangedEnemyScene] = targetPoolSizeRanged;
        if (explodingEnemyScene is not null) targetPoolCounts[explodingEnemyScene] = targetPoolSizeExploding;
        if (tankEnemyScene is not null) targetPoolCounts[tankEnemyScene] = targetPoolSizeTank;
        if (swiftEnemyScene is not null) targetPoolCounts[swiftEnemyScene] = targetPoolSizeSwift;
    }

    private async Task InitializeSinglePoolAsync(PackedScene scene, int targetSize, string poolName)
    {
        if (scene is null)
        {
            GD.PrintErr($"EnemyPoolManager: Cannot initialize pool '{poolName}': Scene is null.");
            return;
        }

        if (!availableEnemies.TryGetValue(scene, out var queue))
        {
            queue = new Queue<BaseEnemy>(targetSize);
            availableEnemies.Add(scene, queue);
        }

        int needed = targetSize - queue.Count;
        int createdCount = 0;
        GD.Print($"EnemyPoolManager: Pool '{poolName}' needs {needed} instances.");

        for (int i = 0; i < needed; i++)
        {
            // Directly create from the specific, concrete scene
            BaseEnemy instance = CreateAndSetupEnemy(scene);
            if (instance is not null)
            {
                queue.Enqueue(instance);
                createdCount++;
            }
            await Task.Yield();
        }
        GD.Print($"EnemyPoolManager: - {poolName} Pool ({scene.ResourcePath}): {queue.Count}/{targetSize} (Added {createdCount})");
    }

    private BaseEnemy CreateAndSetupEnemy(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("EnemyPoolManager: Scene is null, cannot create enemy.");
            return null;
        }

        var enemy = scene.Instantiate<BaseEnemy>();
        if (enemy is null)
        {
            GD.PrintErr($"EnemyPoolManager: Failed to instantiate enemy from scene: {scene.ResourcePath}. Check if the scene's root node script inherits from BaseEnemy.");
            return null;
        }

        enemy.SourceScene = scene;
        enemy.Visible = false;
        enemy.ProcessMode = ProcessModeEnum.Disabled;
        enemy.SetPhysicsProcess(false);
        enemy.Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        AddChild(enemy);
        return enemy;
    }

    public BaseEnemy GetEnemy(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("EnemyPoolManager.GetEnemy: Null scene provided.");
            return null;
        }

        if (!poolsInitialized)
        {
            GD.PushWarning("EnemyPoolManager.GetEnemy called before pools fully initialized! Creating emergency instance.");
            var emergencyEnemy = CreateAndSetupEnemy(scene);
            return emergencyEnemy;
        }

        if (!availableEnemies.TryGetValue(scene, out var queue))
        {
            GD.PrintErr($"EnemyPoolManager.GetEnemy: Pool not found for {scene.ResourcePath}. Was it loaded/initialized? Creating fallback.");
            var fallbackEnemy = CreateAndSetupEnemy(scene);
            return fallbackEnemy;
        }

        BaseEnemy enemy = null;
        while (queue.Count > 0)
        {
            enemy = queue.Dequeue();
            if (enemy is not null && IsInstanceValid(enemy))
            {
                break;
            }
            GD.PrintErr($"EnemyPoolManager: Discarded invalid enemy from pool {scene.ResourcePath}.");
            enemy = null;
        }


        if (enemy is null)
        {
            GD.Print($"EnemyPoolManager: Pool empty or contained only invalid instances for {scene.ResourcePath}! Creating new instance.");
            enemy = CreateAndSetupEnemy(scene);
            if (enemy is null) return null;
        }

        enemy.Visible = false;
        enemy.ProcessMode = ProcessModeEnum.Disabled;
        enemy.SetPhysicsProcess(false);
        enemy.Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        return enemy;
    }

    public void ReturnEnemy(BaseEnemy enemy)
    {
        if (enemy is null || !IsInstanceValid(enemy))
        {
            GD.PrintErr($"EnemyPoolManager.ReturnEnemy: Invalid enemy instance {enemy?.GetInstanceId()}.");
            return;
        }
        if (enemy.SourceScene is null)
        {
            GD.PrintErr($"EnemyPoolManager: Enemy {enemy.GetInstanceId()} cannot return to pool: SourceScene is null. Freeing.");
            enemy.QueueFree();
            return;
        }

        if (!availableEnemies.TryGetValue(enemy.SourceScene, out var queue))
        {
            GD.PrintErr($"EnemyPoolManager: Pool not found for {enemy.SourceScene.ResourcePath} on return. Freeing enemy {enemy.GetInstanceId()}.");
            enemy.QueueFree();
            return;
        }

        enemy.ResetForPooling();

        if (enemy.GetParent() != this)
        {
            enemy.GetParent()?.RemoveChild(enemy);
            AddChild(enemy);
        }

        queue.Enqueue(enemy);
    }

    public void CleanUpActiveObjects()
    {
        GD.Print("EnemyPoolManager: Cleaning up active enemies...");
        var nodesToClean = new List<BaseEnemy>();
        var invalidSpawners = new List<EnemySpawner>();

        foreach (var spawner in _activeSpawners)
        {
            if (spawner is not null && IsInstanceValid(spawner))
            {
                // Iterate over a copy of children collection if ReturnEnemy modifies it by reparenting
                var spawnerChildren = spawner.GetChildren().OfType<Node>().ToList();
                foreach (Node child in spawnerChildren)
                {
                    if (child is BaseEnemy enemy && enemy.ProcessMode != ProcessModeEnum.Disabled)
                    {
                        if (enemy.SourceScene != null && availableEnemies.ContainsKey(enemy.SourceScene))
                        {
                            nodesToClean.Add(enemy);
                        }
                    }
                }
            }
            else
            {
                invalidSpawners.Add(spawner);
            }
        }

        // Remove spawners that are no longer valid
        foreach (var invalidSpawner in invalidSpawners)
        {
            _activeSpawners.Remove(invalidSpawner);
            GD.Print($"EnemyPoolManager: Removed invalid spawner during cleanup.");
        }


        GD.Print($"EnemyPoolManager: Found {nodesToClean.Count} active enemies potentially needing cleanup from spawner children.");

        foreach (var enemy in nodesToClean)
        {
            if (IsInstanceValid(enemy))
            {
                GD.Print($" - Returning active enemy {enemy.GetInstanceId()} from scene {enemy.SourceScene.ResourcePath} to pool.");
                ReturnEnemy(enemy);
            }
        }
        GD.Print("EnemyPoolManager: Finished cleaning active enemies.");
    }
}