using Godot;
using System.Collections.Generic;
using CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for TankEnemy
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5; // Added pool size for TankEnemy
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
		InitializePool(tankEnemyScene, initialPoolSizeTank); // Initialize pool for TankEnemy
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: PackedScene is null. Cannot initialize pool.");
			return;
		}

		if (availableEnemies.ContainsKey(scene))
		{
			GD.PushWarning($"EnemyPoolManager: Pool for scene {scene.ResourcePath} already initialized. Skipping.");
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
			GD.PushError("EnemyPoolManager: Attempted to get enemy with a null scene.");
			return null;
		}

		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Pool not initialized for scene: {scene.ResourcePath}. Instantiating a new enemy directly (not pooled).");

			if (enemyContainer is null)
			{
				GD.PushError("EnemyPoolManager: Cannot instantiate fallback enemy because enemyContainer is null!");
				return null;
			}

			var fallbackEnemy = scene.Instantiate<BaseEnemy>();
			fallbackEnemy.PoolManager = this;
			fallbackEnemy.SourceScene = scene;
			enemyContainer.AddChild(fallbackEnemy);
			return fallbackEnemy;
		}

		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

			if (enemyContainer is null)
			{
				GD.PushError("EnemyPoolManager: Cannot reparent enemy because enemyContainer is null!");
				queue.Enqueue(enemy);
				return null;
			}

			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy);
				enemyContainer.AddChild(enemy);
			}
			return enemy;
		}
		else
		{
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scene.ResourcePath}. Instantiating new enemy.");

			if (enemyContainer is null)
			{
				GD.PushError("EnemyPoolManager: Cannot instantiate new enemy because enemyContainer is null!");
				return null;
			}

			var enemy = scene.Instantiate<BaseEnemy>();
			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			enemyContainer.AddChild(enemy);
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
