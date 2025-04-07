using Godot;
using System.Collections.Generic;
using CosmocrushGD; // Assuming your enemies are in this namespace

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene; // Added export for the new enemy scene
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10; // Added initial pool size for exploding enemy
	[Export] private NodePath enemyContainerPath; // Optional: Node where active enemies will be parented

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer == null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid. Active enemies will be parented to the Pool Manager.");
			enemyContainer = this; // Fallback
		}

		InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		InitializePool(explodingEnemyScene, initialPoolSizeExploding); // Initialize pool for exploding enemy
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene == null)
		{
			GD.PushError($"EnemyPoolManager: PackedScene is null. Cannot initialize pool.");
			return;
		}

		Queue<BaseEnemy> queue = new Queue<BaseEnemy>();
		availableEnemies.Add(scene, queue);

		for (int i = 0; i < count; i++)
		{
			var enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this; // Give enemy a reference back to the pool
			enemy.SourceScene = scene; // Store which scene it came from
			AddChild(enemy); // Add to the PoolManager node initially
			enemy.ProcessMode = ProcessModeEnum.Disabled; // Disable processing
			enemy.Visible = false; // Hide
			if (enemy.Collider != null) enemy.Collider.Disabled = true; // Disable collision
			queue.Enqueue(enemy);
		}
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (!availableEnemies.ContainsKey(scene))
		{
			// This case should ideally not happen anymore if initialization is done correctly
			GD.PushError($"EnemyPoolManager: Attempted to get enemy from uninitialized pool for scene: {scene.ResourcePath}. Instantiating fallback.");
			// Fallback: Instantiate a new one if pool is somehow missing (shouldn't happen with proper init)
			var fallbackEnemy = scene.Instantiate<BaseEnemy>();
			fallbackEnemy.PoolManager = this;
			fallbackEnemy.SourceScene = scene;
			enemyContainer.AddChild(fallbackEnemy); // Add directly to active container
			return fallbackEnemy;
			// return null; // Original behavior was to return null after error
		}

		Queue<BaseEnemy> queue = availableEnemies[scene];

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
			var enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			enemyContainer.AddChild(enemy); // Add directly to active container
			return enemy;
		}
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy == null || enemy.SourceScene == null || !availableEnemies.ContainsKey(enemy.SourceScene))
		{
			GD.PushError("EnemyPoolManager: Cannot return enemy. Invalid enemy or source scene.");
			if (enemy != null && !enemy.IsQueuedForDeletion())
			{
				enemy.QueueFree(); // Clean up if it can't be returned
			}
			return;
		}

		// Disable and hide before reparenting and adding back to queue
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider != null) enemy.Collider.Disabled = true;
		// Stop particles immediately
		if (enemy.DamageParticles != null) enemy.DamageParticles.Emitting = false;
		if (enemy.DeathParticles != null) enemy.DeathParticles.Emitting = false;


		// Reparent back to the PoolManager node to keep the main scene clean
		if (enemy.GetParent() != this)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			AddChild(enemy);
		}


		availableEnemies[enemy.SourceScene].Enqueue(enemy);
	}
}
