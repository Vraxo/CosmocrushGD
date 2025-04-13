using Godot;
using System.Collections.Generic;
using CosmocrushGD; // Assuming your enemies are in this namespace

public partial class EnemyPoolManager : Node
{
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private NodePath enemyContainerPath;

	private Dictionary<string, Queue<BaseEnemy>> availableEnemies = new();
	private Dictionary<string, PackedScene> sceneCache = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		GD.Print("EnemyPoolManager: _Ready starting.");
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer is null)
		{
			GD.PushWarning("EnemyPoolManager: Enemy Container Node not found or invalid. Active enemies will be parented to the Pool Manager.");
			enemyContainer = this; // Fallback
		}

		InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		InitializePool(explodingEnemyScene, initialPoolSizeExploding);
		GD.Print("EnemyPoolManager: _Ready finished.");
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: Provided PackedScene is null. Cannot initialize pool.");
			return;
		}
		if (string.IsNullOrEmpty(scene.ResourcePath))
		{
			GD.PushError($"EnemyPoolManager: Provided PackedScene has no ResourcePath. Cannot initialize pool for scene: {scene}");
			return;
		}

		string scenePath = scene.ResourcePath;
		GD.Print($"EnemyPoolManager: Attempting to initialize pool for '{scenePath}' with {count} instances.");

		if (availableEnemies.ContainsKey(scenePath))
		{
			GD.PushWarning($"EnemyPoolManager: Pool already initialized for scene path: {scenePath}. Skipping re-initialization.");
			return;
		}

		sceneCache[scenePath] = scene;

		Queue<BaseEnemy> queue = new();
		availableEnemies.Add(scenePath, queue);

		int successfulInstantiations = 0;
		for (int i = 0; i < count; i++)
		{
			var enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"Failed to instantiate enemy from scene: {scenePath} on iteration {i}");
				continue;
			}
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
			successfulInstantiations++;
		}
		GD.Print($"EnemyPoolManager: Successfully initialized pool for {scenePath} with {successfulInstantiations}/{count} instances.");
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null || string.IsNullOrEmpty(scene.ResourcePath))
		{
			GD.PushError("EnemyPoolManager: GetEnemy called with a null or pathless scene.");
			return null;
		}

		string scenePath = scene.ResourcePath;
		GD.Print($"EnemyPoolManager: GetEnemy requested for path: '{scenePath}'");

		if (!availableEnemies.TryGetValue(scenePath, out Queue<BaseEnemy> queue))
		{
			// Log the currently initialized keys for debugging
			string initializedKeys = string.Join(", ", availableEnemies.Keys);
			GD.PushError($"EnemyPoolManager: Pool not initialized for scene path: {scenePath}. Available keys: [{initializedKeys}]. Instantiating a new enemy directly (not pooled).");

			var fallbackEnemy = scene.Instantiate<BaseEnemy>();
			if (fallbackEnemy is not null)
			{
				fallbackEnemy.PoolManager = this;
				fallbackEnemy.SourceScene = scene;
				enemyContainer.AddChild(fallbackEnemy);
			}
			else
			{
				GD.PushError($"Failed to instantiate fallback enemy from scene: {scenePath}");
			}
			return fallbackEnemy;
		}


		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();
			GD.Print($"EnemyPoolManager: Reusing enemy '{enemy.Name}' from pool for path: {scenePath}. Pool size now: {queue.Count}");
			if (enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy);
				enemyContainer.AddChild(enemy);
			}
			return enemy;
		}
		else
		{
			GD.PushWarning($"EnemyPoolManager: Pool empty for {scenePath}. Instantiating new enemy.");
			if (!sceneCache.TryGetValue(scenePath, out PackedScene cachedScene))
			{
				GD.PushError($"EnemyPoolManager: Scene not found in cache for path {scenePath}. Cannot instantiate new enemy.");
				return null;
			}
			var enemy = cachedScene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"Failed to instantiate new enemy from cached scene: {scenePath}");
				return null;
			}
			enemy.PoolManager = this;
			enemy.SourceScene = cachedScene;
			enemyContainer.AddChild(enemy);
			GD.Print($"EnemyPoolManager: Instantiated new enemy '{enemy.Name}' for path: {scenePath}.");
			return enemy;
		}
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null)
		{
			GD.PushError("EnemyPoolManager: Attempted to return a null enemy.");
			return;
		}
		if (enemy.SourceScene is null || string.IsNullOrEmpty(enemy.SourceScene.ResourcePath))
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. Invalid SourceScene or ResourcePath.");
			if (!enemy.IsQueuedForDeletion())
			{
				enemy.QueueFree();
			}
			return;
		}

		string scenePath = enemy.SourceScene.ResourcePath;

		if (!availableEnemies.TryGetValue(scenePath, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager: Cannot return enemy. Pool not found for source scene path: {scenePath}. Enemy '{enemy.Name}' will be freed.");
			if (!enemy.IsQueuedForDeletion())
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


		queue.Enqueue(enemy);
		// GD.Print($"EnemyPoolManager: Returned enemy '{enemy.Name}' to pool for path: {scenePath}. Pool size now: {queue.Count}"); // Optional log
	}
}
