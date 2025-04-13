using Godot;
using System.Collections.Generic;
using CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for the new enemy scene
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 8; // Added initial pool size for tank enemy
	[Export] private NodePath enemyContainerPath;

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer is null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid. Active enemies will be parented to the Pool Manager.");
			enemyContainer = this;
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

		Queue<BaseEnemy> queue = new();
		availableEnemies.Add(scene, queue);

		for (int i = 0; i < count; i++)
		{
			var enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			AddChild(enemy);
			enemy.ProcessMode = ProcessModeEnum.Disabled;
			enemy.Visible = false;
			if (enemy.Collider is not null)
			{
				enemy.Collider.Disabled = true;
			}
			queue.Enqueue(enemy);
		}
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError("EnemyPoolManager: Attempted to get enemy with a null PackedScene.");
			return null;
		}


		if (!availableEnemies.ContainsKey(scene))
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy from uninitialized pool for scene: {scene.ResourcePath}. Initializing fallback pool.");
			// Fallback: Initialize a small pool on demand if somehow missed in _Ready
			InitializePool(scene, 5);
			// This should ideally not happen if all scenes are assigned in the editor.
		}

		Queue<BaseEnemy> queue = availableEnemies[scene];

		BaseEnemy enemy = null;
		if (queue.Count > 0)
		{
			enemy = queue.Dequeue();
		}
		else
		{
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath}. Instantiating new enemy.");
			enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			// Add directly to active container, assuming it will be used immediately
			enemyContainer.AddChild(enemy);
			// The ResetState call will happen externally in EnemySpawner
			return enemy;
		}

		// Reparent to the active container before enabling/resetting state externally
		if (enemy.GetParent() != enemyContainer)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			enemyContainer.AddChild(enemy);
		}

		// State reset (visibility, processing, health etc.) is handled by the spawner calling ResetState.
		return enemy;
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null || enemy.SourceScene is null || !availableEnemies.ContainsKey(enemy.SourceScene))
		{
			GD.PushError("EnemyPoolManager: Cannot return enemy. Invalid enemy or source scene.");
			if (enemy is not null && !enemy.IsQueuedForDeletion() && IsInstanceValid(enemy))
			{
				enemy.QueueFree();
			}
			return;
		}

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

		if (enemy.GetParent() != this)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			AddChild(enemy);
		}

		availableEnemies[enemy.SourceScene].Enqueue(enemy);
	}
}
