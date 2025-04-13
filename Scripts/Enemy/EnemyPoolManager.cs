using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private NodePath enemyContainerPath;

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		enemyContainer = GetNodeOrNull<Node>(enemyContainerPath);

		if (enemyContainer is null)
		{
			string pathString = enemyContainerPath is not null
				? enemyContainerPath.ToString()
				: "NULL";
			GD.PrintErr($"EnemyPoolManager: Enemy Container Node not found at path '{pathString}'. Active enemies will be parented to the Pool Manager node '{Name}'.");
			enemyContainer = this;
		}

		InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		InitializePool(explodingEnemyScene, initialPoolSizeExploding);
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
			GD.PushError("EnemyPoolManager.GetEnemy: Attempted to get enemy with a null scene.");
			return null;
		}

		if (enemyContainer is null || !IsInstanceValid(enemyContainer))
		{
			string pathString = enemyContainerPath is not null
				? enemyContainerPath.ToString()
				: "NULL";
			GD.PushWarning($"EnemyPoolManager.GetEnemy: enemyContainer '{pathString}' invalid or null. Attempting recovery.");
			enemyContainer = GetNodeOrNull<Node>(enemyContainerPath);
			if (enemyContainer is null)
			{
				GD.PushError($"EnemyPoolManager.GetEnemy: Recovery failed. Using Pool Manager node '{Name}' as parent.");
				enemyContainer = this;
			}
		}

		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Pool not initialized for scene: {scene.ResourcePath}. Instantiating fallback.");
			var fallbackEnemy = scene.Instantiate<BaseEnemy>();
			fallbackEnemy.PoolManager = this;
			fallbackEnemy.SourceScene = scene;
			enemyContainer.AddChild(fallbackEnemy);
			return fallbackEnemy;
		}

		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

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
			GD.PushError("EnemyPoolManager: Cannot return enemy. Invalid enemy or source scene.");
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
