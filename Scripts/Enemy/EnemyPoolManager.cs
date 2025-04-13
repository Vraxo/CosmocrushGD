using Godot;
using System.Collections.Generic;
using CosmocrushGD;

namespace CosmocrushGD; // Keep the namespace consistent

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for Tank Enemy
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5; // Added initial pool size for Tank Enemy
	[Export] private NodePath enemyContainerPath;

	// Initialize the dictionary earlier
	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		// Get the container node reference in _Ready when the scene tree is stable
		if (enemyContainerPath is not null)
		{
			enemyContainer = GetNode<Node>(enemyContainerPath);
		}

		if (enemyContainer is null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid via path. Active enemies will be parented to the Pool Manager itself.");
			enemyContainer = this; // Fallback to self
		}

		// Initialize pools only after the container is potentially ready
		// Add checks to ensure scenes are actually assigned in the inspector
		if (meleeEnemyScene is not null)
		{
			InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		}
		else
		{
			GD.PushWarning("EnemyPoolManager: MeleeEnemyScene is not assigned in the inspector. Pool not initialized.");
		}

		if (rangedEnemyScene is not null)
		{
			InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		}
		else
		{
			GD.PushWarning("EnemyPoolManager: RangedEnemyScene is not assigned in the inspector. Pool not initialized.");
		}

		if (explodingEnemyScene is not null)
		{
			InitializePool(explodingEnemyScene, initialPoolSizeExploding);
		}
		else
		{
			GD.PushWarning("EnemyPoolManager: ExplodingEnemyScene is not assigned in the inspector. Pool not initialized.");
		}

		if (tankEnemyScene is not null)
		{
			InitializePool(tankEnemyScene, initialPoolSizeTank); // Initialize pool for Tank Enemy
		}
		else
		{
			GD.PushWarning("EnemyPoolManager: TankEnemyScene is not assigned in the inspector. Pool not initialized.");
		}
	}


	private void InitializePool(PackedScene scene, int count)
	{
		// Scene null check already done in _Ready before calling this

		// Check if already initialized (handles potential future logic errors)
		if (availableEnemies.ContainsKey(scene))
		{
			GD.PushWarning($"EnemyPoolManager: Pool for scene '{scene.ResourcePath}' has already been initialized. Skipping duplicate initialization.");
			return;
		}

		Queue<BaseEnemy> queue = new();
		availableEnemies.Add(scene, queue); // Add the key *before* populating the queue

		for (int i = 0; i < count; i++)
		{
			BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate enemy from scene: {scene.ResourcePath}");
				continue;
			}

			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			AddChild(enemy); // Add to the PoolManager node itself initially (safer parent)
			enemy.ProcessMode = ProcessModeEnum.Disabled;
			enemy.Visible = false;
			if (enemy.Collider is not null)
			{
				enemy.Collider.Disabled = true;
			}
			queue.Enqueue(enemy);
		}
		GD.Print($"EnemyPoolManager: Initialized pool for '{scene.ResourcePath}' with {count} instances.");
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy with a null scene.");
			return null;
		}

		// Check if the pool exists for this scene
		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			// Log an error if the pool should have been initialized in _Ready but wasn't
			GD.PushError($"EnemyPoolManager: Attempted to get enemy from an uninitialized or unassigned pool for scene: {scene.ResourcePath}. Check inspector assignments.");
			// Avoid fallback initialization here, as it masks the setup error.
			return null;
		}

		BaseEnemy enemy;
		if (queue.Count > 0)
		{
			enemy = queue.Dequeue();
			// Ensure the enemy is parented correctly before use
			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy); // Remove from PoolManager node
													   // Ensure container is valid before adding
				if (enemyContainer is not null && IsInstanceValid(enemyContainer))
				{
					enemyContainer.AddChild(enemy); // Add to active container
				}
				else
				{
					GD.PushError($"EnemyPoolManager: Cannot reparent enemy to container for scene {scene.ResourcePath}. Container is invalid. Adding to PoolManager instead.");
					AddChild(enemy); // Add back to self as a last resort
				}
			}
		}
		else
		{
			// Pool is initialized but empty, create a new instance
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath}. Instantiating new enemy.");
			enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate fallback enemy from scene: {scene.ResourcePath}");
				return null;
			}
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			// Ensure container is valid before adding
			if (enemyContainer is not null && IsInstanceValid(enemyContainer))
			{
				enemyContainer.AddChild(enemy); // Add directly to active container
			}
			else
			{
				GD.PushError($"EnemyPoolManager: Cannot add new enemy to container for scene {scene.ResourcePath}. Container is invalid. Adding to PoolManager instead.");
				AddChild(enemy); // Add to self as a last resort
			}
		}
		return enemy;
	}


	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null)
		{
			GD.PushWarning("EnemyPoolManager: Attempted to return a null enemy.");
			return;
		}
		// Ensure the enemy is valid before proceeding
		if (!IsInstanceValid(enemy))
		{
			GD.PushWarning("EnemyPoolManager: Attempted to return an invalid (freed?) enemy instance.");
			return;
		}


		if (enemy.SourceScene is null)
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. SourceScene is null. Freeing instead.");
			if (!enemy.IsQueuedForDeletion()) enemy.QueueFree();
			return;
		}
		if (!availableEnemies.TryGetValue(enemy.SourceScene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. No pool found for its SourceScene '{enemy.SourceScene.ResourcePath}'. Freeing instead.");
			if (!enemy.IsQueuedForDeletion()) enemy.QueueFree();
			return;
		}


		// Disable and hide before reparenting and adding back to queue
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider is not null)
		{
			enemy.Collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
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

		// Add back to the correct queue
		queue.Enqueue(enemy);
	}
}
