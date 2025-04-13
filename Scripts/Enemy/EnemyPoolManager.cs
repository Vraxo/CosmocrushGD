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

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Node enemyContainer;

	public override void _Ready()
	{
		GD.Print("--- EnemyPoolManager _Ready: Start ---");
		GD.Print($"Instance Path: {GetPath()}");

		// --- ASSIGN CONTAINER ---
		bool containerFoundViaPath = false;
		if (enemyContainerPath is not null && !enemyContainerPath.IsEmpty)
		{
			enemyContainer = GetNodeOrNull<Node>(enemyContainerPath);
			if (enemyContainer is not null)
			{
				containerFoundViaPath = true;
				GD.Print($"Enemy Container found via path '{enemyContainerPath}'. Node: {enemyContainer.Name}");
			}
			else
			{
				GD.Print($"Attempted to get Enemy Container from path '{enemyContainerPath}' FAILED.");
			}
		}
		else
		{
			GD.Print("Enemy Container Path is not set or empty in Inspector.");
		}

		if (!containerFoundViaPath)
		{
			GD.PrintRich($"[color=yellow]EnemyPoolManager Warning:[/color] Using Pool Manager node itself ({Name}) as the enemy container.");
			enemyContainer = this;
		}

		GD.Print($"Final Enemy Container assigned: {(enemyContainer is null ? "NULL" : enemyContainer.GetPath())}");
		if (enemyContainer is null)
		{
			GD.PrintErr("CRITICAL: Enemy Container is NULL after attempting assignment and fallback.");
			// Stop initialization if container assignment failed completely
			GD.Print("--- EnemyPoolManager _Ready: End (Container Error) ---");
			return;
		}
		// --- END CONTAINER ASSIGNMENT ---


		// --- CHECK SCENES ---
		GD.Print($"Melee Enemy Scene: {(meleeEnemyScene is null ? "NULL" : meleeEnemyScene.ResourcePath)}");
		GD.Print($"Ranged Enemy Scene: {(rangedEnemyScene is null ? "NULL" : rangedEnemyScene.ResourcePath)}");
		GD.Print($"Exploding Enemy Scene: {(explodingEnemyScene is null ? "NULL" : explodingEnemyScene.ResourcePath)}");

		bool canInitialize = true;
		if (meleeEnemyScene is null)
		{
			GD.PrintErr("EnemyPoolManager: MeleeEnemyScene is not assigned. Halting initialization.");
			canInitialize = false;
		}
		if (rangedEnemyScene is null)
		{
			GD.PrintErr("EnemyPoolManager: RangedEnemyScene is not assigned. Halting initialization.");
			canInitialize = false;
		}
		if (explodingEnemyScene is null)
		{
			GD.PrintErr("EnemyPoolManager: ExplodingEnemyScene is not assigned. Halting initialization.");
			canInitialize = false;
		}

		if (!canInitialize)
		{
			GD.Print("--- EnemyPoolManager _Ready: End (Initialization Halted) ---");
			return;
		}
		// --- END CHECK SCENES ---

		// --- INITIALIZE POOLS ---
		GD.Print("Proceeding with pool initialization...");
		InitializePool(meleeEnemyScene, initialPoolSizeMelee);
		InitializePool(rangedEnemyScene, initialPoolSizeRanged);
		InitializePool(explodingEnemyScene, initialPoolSizeExploding);

		GD.Print($"EnemyPoolManager initialized. Pool keys count: {availableEnemies.Count}");
		GD.Print("--- EnemyPoolManager _Ready: End (Initialization Complete) ---");
		// --- END INITIALIZE POOLS ---
	}

	private void InitializePool(PackedScene scene, int count)
	{
		if (scene is null)
		{
			GD.PushError($"EnemyPoolManager: InitializePool called with null PackedScene. Skipping.");
			return;
		}
		if (availableEnemies.ContainsKey(scene))
		{
			GD.PushWarning($"EnemyPoolManager: Attempted to initialize pool for scene '{scene.ResourcePath}' which already exists. Skipping duplicate initialization.");
			return;
		}

		Queue<BaseEnemy> queue = new();
		availableEnemies.Add(scene, queue);

		for (int i = 0; i < count; i++)
		{
			BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate enemy from scene '{scene.ResourcePath}' during pool initialization. Skipping this instance.");
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
		}
		GD.Print($"Initialized pool for {scene.ResourcePath} with {count} instances.");
	}

	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PushError("EnemyPoolManager: GetEnemy called with a null PackedScene!");
			return null;
		}

		// Check container validity first
		if (enemyContainer is null)
		{
			GD.PushError($"CRITICAL: EnemyPoolManager.GetEnemy ({GetPath()}): enemyContainer is null at the start of the method!");
			return null;
		}


		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			GD.PushError($"EnemyPoolManager ({GetPath()}): Pool not initialized for scene path: {scene.ResourcePath}. Available keys: [{string.Join(", ", availableEnemies.Keys)}]. Instantiating fallback.");

			var fallbackEnemy = scene.Instantiate<BaseEnemy>();
			if (fallbackEnemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate fallback enemy from scene '{scene.ResourcePath}'.");
				return null;
			}

			fallbackEnemy.PoolManager = this;
			fallbackEnemy.SourceScene = scene;
			GD.Print($"   >> Adding fallback child to container: {enemyContainer.GetPath()}");
			enemyContainer.AddChild(fallbackEnemy);
			return fallbackEnemy;
		}


		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

			if (enemy.GetParent() != enemyContainer)
			{
				Node currentParent = enemy.GetParent();
				if (currentParent is not null)
				{
					currentParent.RemoveChild(enemy);
				}
				GD.Print($"   >> Adding pooled child to container: {enemyContainer.GetPath()}");
				enemyContainer.AddChild(enemy);
			}
			return enemy;
		}
		else
		{
			GD.PushWarning($"EnemyPoolManager ({GetPath()}): Pool empty for {scene.ResourcePath}. Instantiating new enemy.");

			var enemy = scene.Instantiate<BaseEnemy>();
			if (enemy is null)
			{
				GD.PushError($"EnemyPoolManager: Failed to instantiate new enemy from scene '{scene.ResourcePath}'.");
				return null;
			}

			enemy.PoolManager = this;
			enemy.SourceScene = scene;
			GD.Print($"   >> Adding new instance child to container: {enemyContainer.GetPath()}");
			enemyContainer.AddChild(enemy);
			return enemy;
		}
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
			GD.PushError($"EnemyPoolManager: Cannot return enemy '{enemy.Name}'. Pool for scene '{enemy.SourceScene.ResourcePath}' does not exist. Queueing free.");
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
			Node currentParent = enemy.GetParent();
			if (currentParent is not null && IsInstanceValid(currentParent))
			{
				currentParent.RemoveChild(enemy);
			}
			AddChild(enemy);
		}

		queue.Enqueue(enemy);
	}
}
