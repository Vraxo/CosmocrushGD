using Godot;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	private enum SpawnEdge { Top, Right, Bottom, Left }

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added export for Tank Enemy
	[Export] private Vector2 spawnMargin = new(100, 100);
	[Export] private NodePath playerPath;
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;
	[Export] private Vector2 worldSize = new(2272, 1208);
	[Export] private EnemyPoolManager enemyPoolManager;
	[Export] private int initialSpawnCount = 3;

	private Player player;
	private float timeElapsed;
	private const int MaxSpawnAttempts = 10;
	private readonly RandomNumberGenerator rng = new();

	public override void _Ready()
	{
		rng.Randomize();

		// Get references first
		if (playerPath is not null)
		{
			player = GetNode<Player>(playerPath);
		}

		if (player is null)
		{
			GD.PrintErr("EnemySpawner: Player node not found! Spawner disabled.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}
		if (enemyPoolManager is null)
		{
			// Attempt to find it if not assigned? Or rely strictly on export.
			// For now, stick to export.
			GD.PrintErr("EnemySpawner: EnemyPoolManager reference not set in Inspector! Spawner disabled.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		// Setup timers immediately
		SetupTimers();

		// Defer initial spawning until other nodes are likely ready
		CallDeferred(nameof(SpawnInitialEnemies));
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
			GD.PrintErr("EnemySpawner: Timer references not set! Make sure Timers are assigned in the Inspector.");
			// Disable spawning if timers are crucial and missing
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		spawnTimer.Timeout += OnSpawnTimerTimeout;
		rateIncreaseTimer.Timeout += RateIncrease;
		spawnTimer.WaitTime = CalculateSpawnInterval();
		spawnTimer.Start();
		rateIncreaseTimer.Start();
	}

	private void SpawnInitialEnemies()
	{
		// Check if the required scenes for *any* spawning are assigned
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: No enemy scenes assigned. Cannot perform initial spawn.");
			return;
		}

		GD.Print("EnemySpawner: Starting initial enemy spawn...");
		int spawnedCount = 0;
		for (int i = 0; i < initialSpawnCount; i++)
		{
			// SpawnRandomEnemy handles internal checks for assigned scenes now
			if (SpawnRandomEnemy()) // Modify SpawnRandomEnemy to return bool success
			{
				spawnedCount++;
			}
		}
		GD.Print($"EnemySpawner: Initial spawn attempt finished. Successfully spawned {spawnedCount} enemies.");
	}

	private void RateIncrease()
	{
		timeElapsed += (float)rateIncreaseTimer.WaitTime;
		spawnTimer.WaitTime = CalculateSpawnInterval();
	}

	private float CalculateSpawnInterval()
	{
		float calculatedInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
		return Mathf.Clamp(calculatedInterval, minSpawnInterval, baseSpawnRate);
	}

	private void OnSpawnTimerTimeout()
	{
		SpawnRandomEnemy();
	}

	// Modified to return true if spawning was attempted (doesn't guarantee success getting from pool)
	private bool SpawnRandomEnemy()
	{
		// We already check enemyPoolManager and player in _Ready
		// Need to check if *at least one* enemy type is available to spawn
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: Cannot spawn random enemy, no scenes are assigned.");
			return false;
		}


		PackedScene selectedScene = SelectRandomEnemyScene();
		if (selectedScene is not null)
		{
			TrySpawnEnemyFromPool(selectedScene);
			return true; // Spawning was attempted
		}
		else
		{
			GD.PrintErr("EnemySpawner: SelectRandomEnemyScene returned null. This shouldn't happen if at least one scene is assigned.");
			return false; // Failed to select a scene
		}
	}

	private PackedScene SelectRandomEnemyScene()
	{
		// Build a list of available scenes to choose from
		var availableScenes = new Godot.Collections.Array<PackedScene>();
		if (meleeEnemyScene is not null) availableScenes.Add(meleeEnemyScene);
		if (rangedEnemyScene is not null) availableScenes.Add(rangedEnemyScene);
		if (explodingEnemyScene is not null) availableScenes.Add(explodingEnemyScene);
		if (tankEnemyScene is not null) availableScenes.Add(tankEnemyScene);

		if (availableScenes.Count == 0)
		{
			return null; // No scenes to choose from
		}

		// Select a random scene from the available ones
		int randomIndex = rng.RandiRange(0, availableScenes.Count - 1);
		return availableScenes[randomIndex];
	}

	private void TrySpawnEnemyFromPool(PackedScene enemyScene)
	{
		// enemyScene null check done in SpawnRandomEnemy before calling this

		bool foundValidPosition = false;
		int attempts = 0;
		Vector2 spawnPosition = Vector2.Zero;

		while (!foundValidPosition && attempts < MaxSpawnAttempts)
		{
			spawnPosition = GetRandomEdgePosition();

			if (IsPositionValid(spawnPosition))
			{
				foundValidPosition = true;
			}

			attempts++;
		}

		if (foundValidPosition)
		{
			// Ensure pool manager is still valid
			if (enemyPoolManager is null || !IsInstanceValid(enemyPoolManager))
			{
				GD.PrintErr("EnemySpawner: EnemyPoolManager became invalid before spawning. Aborting spawn.");
				return;
			}

			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				// Check if the enemy instance itself is valid before resetting
				if (IsInstanceValid(enemy))
				{
					enemy.ResetState(spawnPosition);
				}
				else
				{
					GD.PrintErr($"EnemySpawner: Got an invalid enemy instance from pool for scene {enemyScene.ResourcePath}.");
				}
			}
			else
			{
				// GetEnemy already logs errors if it returns null
				GD.Print($"EnemySpawner: Failed to get enemy of type {enemyScene.ResourcePath} from pool (PoolManager returned null).");
			}
		}
		else
		{
			GD.Print($"EnemySpawner: Failed to find valid spawn position for {enemyScene.ResourcePath} after {MaxSpawnAttempts} attempts.");
		}
	}

	private bool IsPositionValid(Vector2 position)
	{
		// Ensure player is still valid before checking distance
		if (player is null || !IsInstanceValid(player))
		{
			// Player might have been destroyed, stop spawning relative to it
			// GD.Print("EnemySpawner.IsPositionValid: Player reference is null or invalid.");
			return false; // Cannot determine validity without player
		}
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance;
	}

	private Vector2 GetRandomEdgePosition()
	{
		SpawnEdge edge = (SpawnEdge)rng.RandiRange(0, 3);

		float x = 0f;
		float y = 0f;

		// Ensure worldSize is positive to avoid issues with RandfRange
		float worldWidth = Mathf.Max(0f, worldSize.X);
		float worldHeight = Mathf.Max(0f, worldSize.Y);
		float marginX = Mathf.Max(0f, spawnMargin.X);
		float marginY = Mathf.Max(0f, spawnMargin.Y);


		switch (edge)
		{
			case SpawnEdge.Top:
				x = rng.RandfRange(marginX, worldWidth - marginX);
				y = rng.RandfRange(0, marginY);
				break;
			case SpawnEdge.Right:
				x = rng.RandfRange(worldWidth - marginX, worldWidth);
				y = rng.RandfRange(marginY, worldHeight - marginY);
				break;
			case SpawnEdge.Bottom:
				x = rng.RandfRange(marginX, worldWidth - marginX);
				y = rng.RandfRange(worldHeight - marginY, worldHeight);
				break;
			case SpawnEdge.Left:
				x = rng.RandfRange(0, marginX);
				y = rng.RandfRange(marginY, worldHeight - marginY);
				break;
		}

		// Clamp again just in case margins are larger than half the world size
		x = Mathf.Clamp(x, 0, worldWidth);
		y = Mathf.Clamp(y, 0, worldHeight);

		return new Vector2(x, y);
	}
}
