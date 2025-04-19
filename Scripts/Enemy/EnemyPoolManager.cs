using Godot;
using System.Collections.Generic;
using System.Linq; // Required for Linq operations

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene;
	[Export] private PackedScene swiftEnemyScene;
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5;
	[Export] private int initialPoolSizeSwift = 15;
	[Export] private NodePath enemyContainerPath;

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Dictionary<PackedScene, int> targetPoolCounts = new();
	private List<PackedScene> scenesToInitialize = new();
	private Node enemyContainer;
	private bool initializationComplete = false;

	public override void _Ready()
	{
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer is null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid. Active enemies will be parented to the Pool Manager.");
			enemyContainer = this;
		}

		SetupPool(meleeEnemyScene, initialPoolSizeMelee);
		SetupPool(rangedEnemyScene, initialPoolSizeRanged);
		SetupPool(explodingEnemyScene, initialPoolSizeExploding);
		SetupPool(tankEnemyScene, initialPoolSizeTank);
		SetupPool(swiftEnemyScene, initialPoolSizeSwift);

		SetProcess(true); // Start the _Process loop for initialization
	}

	private void SetupPool(PackedScene scene, int count)
	{
		if (scene is null || count <= 0)
		{
			return;
		}

		if (!availableEnemies.ContainsKey(scene))
		{
			availableEnemies.Add(scene, new Queue<BaseEnemy>());
			targetPoolCounts.Add(scene, count);
			scenesToInitialize.Add(scene);
			GD.Print($"EnemyPoolManager: Queued initialization for {scene.ResourcePath} with target {count} instances.");
		}
		else
		{
			GD.PushWarning($"EnemyPoolManager: Pool setup already exists for scene: {scene.ResourcePath}. Skipping.");
		}
	}

	public override void _Process(double delta)
	{
		if (initializationComplete)
		{
			SetProcess(false); // Stop processing once done
			return;
		}

		bool didInitializeThisFrame = false;
		// Iterate through a copy in case we remove items
		foreach (var scene in scenesToInitialize.ToList())
		{
			if (!availableEnemies.TryGetValue(scene, out var queue) || !targetPoolCounts.TryGetValue(scene, out var targetCount))
			{
				continue; // Should not happen based on SetupPool logic
			}

			if (queue.Count < targetCount)
			{
				InstantiateAndPoolEnemy(scene, queue);
				didInitializeThisFrame = true;
				break; // Only do one per frame to spread the load
			}
		}

		// Check if all pools are filled
		if (!didInitializeThisFrame)
		{
			bool allDone = true;
			foreach (var kvp in targetPoolCounts)
			{
				if (availableEnemies.TryGetValue(kvp.Key, out var queue) && queue.Count < kvp.Value)
				{
					allDone = false;
					break;
				}
			}

			if (allDone)
			{
				initializationComplete = true;
				GD.Print("EnemyPoolManager: All pools initialized.");
				SetProcess(false); // Stop processing
			}
		}
	}

	private void InstantiateAndPoolEnemy(PackedScene scene, Queue<BaseEnemy> queue)
	{
		BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
		enemy.PoolManager = this;
		enemy.SourceScene = scene;
		AddChild(enemy); // Add to the pool manager itself initially
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider is not null)
		{
			enemy.Collider.Disabled = true;
		}
		queue.Enqueue(enemy);
		// GD.Print($"Initialized one {scene.ResourcePath}, current count: {queue.Count}"); // Optional detailed log
	}


	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy with a null PackedScene reference.");
			return null;
		}

		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			// This case should ideally not happen if SetupPool was called for this scene
			GD.PushError($"EnemyPoolManager: Pool not set up for scene: {scene.ResourcePath}. Was it added to exports and SetupPool called?");
			return InstantiateNewEnemy(scene); // Fallback: instantiate directly
		}

		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

			if (enemy is null || !IsInstanceValid(enemy))
			{
				GD.PushWarning($"EnemyPoolManager: Found an invalid enemy instance in the pool for {scene.ResourcePath}. Removing and creating new.");
				return InstantiateNewEnemy(scene); // Instantiate a new one instead
			}


			// Reparent to the container if necessary
			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy);
				if (enemyContainer is null)
				{
					GD.PushError($"EnemyPoolManager: enemyContainer is null when trying to reparent existing enemy for scene: {enemy.SourceScene.ResourcePath}. Adding to PoolManager node instead.");
					AddChild(enemy); // Add to self as fallback container
				}
				else
				{
					enemyContainer.AddChild(enemy);
				}
			}
			return enemy;
		}
		else
		{
			// Pool might be initializing or genuinely empty
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath} (may still be initializing). Instantiating new enemy as fallback.");
			return InstantiateNewEnemy(scene);
		}
	}

	private BaseEnemy InstantiateNewEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: Cannot instantiate new enemy, PackedScene is null.");
			return null;
		}

		BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
		enemy.PoolManager = this;
		enemy.SourceScene = scene;

		if (enemyContainer is null)
		{
			GD.PushError($"EnemyPoolManager: enemyContainer is null when trying to add new enemy for scene: {scene.ResourcePath}. Adding to PoolManager node instead.");
			AddChild(enemy); // Add to self as fallback
		}
		else
		{
			enemyContainer.AddChild(enemy);
		}
		return enemy;
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			GD.PushWarning("EnemyPoolManager: Attempted to return a null or invalid enemy instance.");
			return;
		}

		if (enemy.SourceScene is null)
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. SourceScene is null. Queueing free.");
			enemy.QueueFree();
			return;
		}

		if (!availableEnemies.TryGetValue(enemy.SourceScene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. Pool not found for scene {enemy.SourceScene.ResourcePath}. Queueing free.");
			enemy.QueueFree(); // Don't know where to put it
			return;
		}

		// Reset state common to pooling
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider is not null)
		{
			enemy.Collider.Disabled = true;
		}
		if (enemy.DamageParticles is not null)
		{
			enemy.DamageParticles.Emitting = false;
		}
		if (enemy.DeathParticles is not null)
		{
			enemy.DeathParticles.Emitting = false;
		}

		// Reparent back to the PoolManager node itself to keep inactive nodes organized
		if (enemy.GetParent() != this)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			AddChild(enemy);
		}

		queue.Enqueue(enemy);
	}
}
