using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for TankEnemy scene
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5; // Added export for TankEnemy pool size
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
		InitializePool(tankEnemyScene, initialPoolSizeTank); // Added initialization for TankEnemy pool
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene is null)
		{
			GD.PrintErr($"EnemyPoolManager: PackedScene is null. Cannot initialize pool.");
			return;
		}

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
		GD.Print($"EnemyPoolManager: Initialized pool for {scene.ResourcePath} with {count} instances.");
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy from uninitialized pool for scene: {scene.ResourcePath}. Check if the scene is assigned in the EnemyPoolManager inspector properties and if the pool was initialized correctly. Instantiating fallback.");

			BaseEnemy fallbackEnemy = scene.Instantiate<BaseEnemy>();
			fallbackEnemy.PoolManager = this;
			fallbackEnemy.SourceScene = scene;

			if (enemyContainer is null)
			{
				GD.PushError($"EnemyPoolManager: enemyContainer is null when trying to add fallback enemy for scene: {scene.ResourcePath}. Adding to PoolManager node instead.");
				AddChild(fallbackEnemy);
			}
			else
			{
				enemyContainer.AddChild(fallbackEnemy);
			}
			return fallbackEnemy;
		}

		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy);

				if (enemyContainer is null)
				{
					GD.PushError($"EnemyPoolManager: enemyContainer is null when trying to reparent existing enemy for scene: {enemy.SourceScene.ResourcePath}. Adding to PoolManager node instead.");
					AddChild(enemy);
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
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath}. Instantiating new enemy.");
			BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this;
			enemy.SourceScene = scene;

			if (enemyContainer is null)
			{
				GD.PushError($"EnemyPoolManager: enemyContainer is null when trying to add new enemy for scene: {scene.ResourcePath}. Adding to PoolManager node instead.");
				AddChild(enemy);
			}
			else
			{
				enemyContainer.AddChild(enemy);
			}
			return enemy;
		}
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null || enemy.SourceScene is null || !availableEnemies.ContainsKey(enemy.SourceScene))
		{
			GD.PushError("EnemyPoolManager: Cannot return enemy. Invalid enemy, source scene, or pool not found.");
			if (enemy is not null && !enemy.IsQueuedForDeletion())
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
