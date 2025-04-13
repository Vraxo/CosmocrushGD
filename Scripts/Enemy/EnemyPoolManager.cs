using Godot;
using System.Collections.Generic;
using CosmocrushGD; // Assuming your enemies are in this namespace

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for Tank Enemy
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 8; // Added initial pool size for Tank Enemy
	[Export] private NodePath enemyContainerPath; // Optional: Node where active enemies will be parented

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer is null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid. Active enemies will be parented to the Pool Manager.");
			enemyContainer = this; // Fallback
		}

		InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		InitializePool(explodingEnemyScene, initialPoolSizeExploding);
		InitializePool(tankEnemyScene, initialPoolSizeTank); // Initialize pool for tank enemy
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: PackedScene is null. Cannot initialize pool.");
			return;
		}

		// Check if pool already exists for this scene (e.g., if _Ready is called multiple times)
		if (availableEnemies.ContainsKey(scene))
		{
			GD.PushWarning($"EnemyPoolManager: Pool already initialized for scene: {scene.ResourcePath}. Skipping.");
			return;
		}

		Queue<BaseEnemy> queue = new();
		availableEnemies.Add(scene, queue);

		for (int i = 0; i < count; i++)
		{
			BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate enemy from scene: {scene.ResourcePath}");
				continue;
			}
			enemy.PoolManager = this; // Give enemy a reference back to the pool
			enemy.SourceScene = scene; // Store which scene it came from
			AddChild(enemy); // Add to the PoolManager node initially
			enemy.ProcessMode = ProcessModeEnum.Disabled; // Disable processing
			enemy.Visible = false; // Hide
			if (enemy.Collider is not null)
			{
				enemy.Collider.Disabled = true; // Disable collision
			}
			queue.Enqueue(enemy);
		}
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError("EnemyPoolManager: Attempted to get enemy with a null scene!");
			return null;
		}

		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy from uninitialized pool for scene: {scene.ResourcePath}. Initializing fallback pool.");
			// Initialize a small pool on the fly as a fallback
			InitializePool(scene, 5); // Initialize with a small default count
			if (!availableEnemies.TryGetValue(scene, out queue))
			{
				// If initialization still failed, something is seriously wrong
				GD.PushError($"EnemyPoolManager: Fallback pool initialization failed for {scene.ResourcePath}. Returning null.");
				return null;
			}
		}


		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();
			// Reparent to the active container before enabling
			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy); // Remove from PoolManager node
				enemyContainer.AddChild(enemy); // Add to active container
			}
			return enemy;
		}
		else
		{
			// Optional: Instantiate a new one if pool is empty
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath}. Instantiating new enemy.");
			BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate fallback enemy from scene: {scene.ResourcePath}");
				return null;
			}
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			enemyContainer.AddChild(enemy); // Add directly to active container
			return enemy;
		}
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null || enemy.SourceScene is null || !availableEnemies.ContainsKey(enemy.SourceScene))
		{
			GD.PushError("EnemyPoolManager: Cannot return enemy. Invalid enemy, source scene, or pool not initialized for this type.");
			if (enemy is not null && !enemy.IsQueuedForDeletion())
			{
				enemy.QueueFree(); // Clean up if it can't be returned
			}
			return;
		}

		// Disable and hide before reparenting and adding back to queue
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider is not null)
		{
			enemy.Collider.Disabled = true;
		}
		// Stop particles immediately
		if (enemy.DamageParticles is not null)
		{
			enemy.DamageParticles.Emitting = false;
		}
		if (enemy.DeathParticles is not null)
		{
			enemy.DeathParticles.Emitting = false;
		}


		// Reparent back to the PoolManager node to keep the main scene clean
		if (enemy.GetParent() != this)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			AddChild(enemy);
		}


		availableEnemies[enemy.SourceScene].Enqueue(enemy);
	}
}
