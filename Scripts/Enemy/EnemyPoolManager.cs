using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene;
	[Export] private PackedScene swiftEnemyScene; // Added SwiftEnemy scene export
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5;
	[Export] private int initialPoolSizeSwift = 15; // Added SwiftEnemy pool size export
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
		InitializePool(tankEnemyScene, initialPoolSizeTank);
		InitializePool(swiftEnemyScene, initialPoolSizeSwift); // Added initialization for SwiftEnemy pool
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
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: Attempted to get enemy with a null PackedScene reference.");
			return null;
		}

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

			if (enemy is null || !IsInstanceValid(enemy))
			{
				GD.PushWarning($"EnemyPoolManager: Found an invalid enemy instance in the pool for {scene.ResourcePath}. Removing and creating new.");
				// Attempt to instantiate a new one instead of returning null
				return InstantiateNewEnemy(scene);
			}


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
			AddChild(enemy);
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
			enemy.QueueFree();
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

		queue.Enqueue(enemy);
	}
}
