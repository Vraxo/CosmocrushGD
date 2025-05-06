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

		// Instantiate the specific enemy scene (MeleeEnemy.tscn, RangedEnemy.tscn, etc.)
		// This scene should have the concrete script (MeleeEnemy.cs, RangedEnemy.cs) attached,
		// NOT the abstract BaseEnemy.cs.
		var enemy = scene.Instantiate<BaseEnemy>();
		if (enemy is null)
		{
			// This might happen if the scene root node doesn't have a script inheriting BaseEnemy
			GD.PrintErr($"EnemyPoolManager: Failed to instantiate enemy from scene: {scene.ResourcePath}. Check if the scene's root node script inherits from BaseEnemy.");
			return null;
		}

		// Setup common properties for pooling
		enemy.SourceScene = scene;
		enemy.Visible = false;
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.SetPhysicsProcess(false);
		enemy.Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		AddChild(enemy); // Add to the pool manager node
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
			// Note: ResetAndActivate should be called by the spawner after getting the enemy
			return emergencyEnemy;
		}

		if (!availableEnemies.TryGetValue(scene, out var queue))
		{
			GD.PrintErr($"EnemyPoolManager.GetEnemy: Pool not found for {scene.ResourcePath}. Was it loaded/initialized? Creating fallback.");
			var fallbackEnemy = CreateAndSetupEnemy(scene);
			return fallbackEnemy;
		}

		BaseEnemy enemy = null;
		while (queue.Count > 0) // Check for and discard invalid instances
		{
			enemy = queue.Dequeue();
			if (enemy is not null && IsInstanceValid(enemy))
			{
				break; // Found a valid instance
			}
			GD.PrintErr($"EnemyPoolManager: Discarded invalid enemy from pool {scene.ResourcePath}.");
			enemy = null; // Reset enemy to null if invalid
		}


		if (enemy is null) // If no valid instance was found or pool was empty
		{
			GD.Print($"EnemyPoolManager: Pool empty or contained only invalid instances for {scene.ResourcePath}! Creating new instance.");
			enemy = CreateAndSetupEnemy(scene);
			if (enemy is null) return null; // Creation failed
		}

		// Basic reset before returning - Spawner is responsible for full ResetAndActivate
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

		enemy.ResetForPooling(); // Call the enemy's own reset method

		// Reparent back to the pool manager if it's not already a child
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

		// Search the entire scene tree for active enemies managed by this pool
		FindActiveEnemies(GetTree().Root, nodesToClean);

		GD.Print($"EnemyPoolManager: Found {nodesToClean.Count} active enemies potentially needing cleanup.");

		foreach (var enemy in nodesToClean)
		{
			// Check validity and if it belongs to a known pool
			if (IsInstanceValid(enemy) && enemy.SourceScene != null && availableEnemies.ContainsKey(enemy.SourceScene))
			{
				// Ensure it's not already a child of the pool manager (could happen if returned but cleanup is called before next frame)
				if (enemy.GetParent() != this)
				{
					GD.Print($" - Returning active enemy {enemy.GetInstanceId()} from scene {enemy.SourceScene.ResourcePath} to pool.");
					ReturnEnemy(enemy);
				}
			}
		}
		GD.Print("EnemyPoolManager: Finished cleaning active enemies.");
	}

	// Helper to recursively find active enemies in the scene tree
	private void FindActiveEnemies(Node startNode, List<BaseEnemy> activeList)
	{
		// Check if the current node is an enemy managed by this pool and is currently active
		if (startNode is BaseEnemy enemy && enemy.ProcessMode != ProcessModeEnum.Disabled)
		{
			// Ensure it has a source scene and that scene is one we manage pools for
			if (enemy.SourceScene != null && availableEnemies.ContainsKey(enemy.SourceScene))
			{
				// Ensure it's not already parented to the pool manager (meaning it's truly 'active' in the game world)
				if (enemy.GetParent() != this)
				{
					activeList.Add(enemy);
				}
			}
		}

		// Recursively check children
		foreach (Node child in startNode.GetChildren())
		{
			FindActiveEnemies(child, activeList);
		}
	}
}
