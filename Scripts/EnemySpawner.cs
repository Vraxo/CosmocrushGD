using System;
using Godot;
using CosmocrushGD; // Assuming your enemies are in this namespace

public partial class EnemySpawner : Node
{
	private enum SpawnEdge { Top, Right, Bottom, Left }

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene; // Added export for the new enemy
	[Export] private Vector2 spawnMargin = new(100, 100);
	[Export] private NodePath playerPath;
	// Remove enemyContainerPath export here, it's managed by the Pool Manager now
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;
	[Export] private Vector2 worldSize = new(2272, 1208);

	// Add reference to the Pool Manager
	[Export] private EnemyPoolManager enemyPoolManager;

	private Player player;
	// private bool spawnMeleeNext = true; // Removed old alternating logic
	private float timeElapsed;
	private const int MaxSpawnAttempts = 10;
	private RandomNumberGenerator rng = new RandomNumberGenerator(); // Added for random selection

	public override void _Ready()
	{
		rng.Randomize(); // Initialize RNG
		player = GetNode<Player>(playerPath);

		if (player is null)
		{
			GD.PrintErr("Player node not found!");
			// Consider disabling spawner if player is missing SetProcess(false);
		}
		if (enemyPoolManager is null)
		{
			GD.PrintErr("EnemyPoolManager reference not set in EnemySpawner!");
			// Consider disabling spawner SetProcess(false);
		}


		SetupTimers();
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
			GD.PrintErr("Timer references not set!");
			return;
		}

		spawnTimer.Timeout += OnSpawnTimerTimeout; // Renamed to avoid conflict
		rateIncreaseTimer.Timeout += RateIncrease;
	}

	private void RateIncrease()
	{
		timeElapsed += (float)rateIncreaseTimer.WaitTime; // Use timer wait time for accuracy
		spawnTimer.WaitTime = CalculateSpawnInterval();
	}

	private float CalculateSpawnInterval()
	{
		float calculatedInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
		return Mathf.Clamp(calculatedInterval, minSpawnInterval, baseSpawnRate);
	}

	// Renamed from SpawnEnemy to avoid confusion with the old spawning logic
	private void OnSpawnTimerTimeout()
	{
		// Check all three scenes now
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null || player is null || enemyPoolManager is null)
		{
			GD.PrintErr("Missing required references (including ExplodingEnemyScene) in EnemySpawner!");
			return;
		}

		// Randomly select one of the three enemy types
		PackedScene selectedScene;
		int enemyType = rng.RandiRange(0, 2); // 0: Melee, 1: Ranged, 2: Exploding

		switch (enemyType)
		{
			case 0:
				selectedScene = meleeEnemyScene;
				break;
			case 1:
				selectedScene = rangedEnemyScene;
				break;
			case 2:
			default: // Default to exploding just in case
				selectedScene = explodingEnemyScene;
				break;
		}

		TrySpawnEnemyFromPool(selectedScene);
	}

	// Renamed from TrySpawnEnemy
	private void TrySpawnEnemyFromPool(PackedScene enemyScene)
	{
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
			// Get enemy from pool instead of instantiating
			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy != null)
			{
				// Reset and position the enemy
				enemy.ResetState(spawnPosition);
				// No AddChild needed here, PoolManager handles parenting
			}
			else
			{
				GD.Print($"Failed to get enemy of type {enemyScene.ResourcePath} from pool.");
			}

		}
		else
		{
			GD.Print("Failed to find valid spawn position after multiple attempts.");
		}
	}

	private bool IsPositionValid(Vector2 position)
	{
		if (player == null || !IsInstanceValid(player)) return false; // Check if player is valid
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance; // Use DistanceSquaredTo for slight optimization
	}

	private Vector2 GetRandomEdgePosition()
	{
		// Use RandomNumberGenerator for better randomness control if needed
		// var rng = new RandomNumberGenerator(); rng.Randomize(); rng.RandfRange(...)
		SpawnEdge edge = (SpawnEdge)(GD.Randi() % 4);

		float x = 0f, y = 0f; // Initialize with default

		switch (edge)
		{
			case SpawnEdge.Top:
				x = (float)GD.RandRange(spawnMargin.X, worldSize.X - spawnMargin.X);
				y = (float)GD.RandRange(0, spawnMargin.Y);
				break;
			case SpawnEdge.Right:
				x = (float)GD.RandRange(worldSize.X - spawnMargin.X, worldSize.X);
				y = (float)GD.RandRange(spawnMargin.Y, worldSize.Y - spawnMargin.Y);
				break;
			case SpawnEdge.Bottom:
				x = (float)GD.RandRange(spawnMargin.X, worldSize.X - spawnMargin.X);
				y = (float)GD.RandRange(worldSize.Y - spawnMargin.Y, worldSize.Y);
				break;
			case SpawnEdge.Left:
				x = (float)GD.RandRange(0, spawnMargin.X);
				y = (float)GD.RandRange(spawnMargin.Y, worldSize.Y - spawnMargin.Y);
				break;
				// No default needed as SpawnEdge enum covers all Randi % 4 results
		}

		// Clamp values just in case RandRange behaves unexpectedly at edges or worldSize/margins are zero
		x = Mathf.Clamp(x, 0, worldSize.X);
		y = Mathf.Clamp(y, 0, worldSize.Y);

		return new Vector2(x, y);
	}

	// Remove the old SpawnEnemy(PackedScene scene, Vector2 position) method
	// private void SpawnEnemy(PackedScene scene, Vector2 position) { ... }
}
