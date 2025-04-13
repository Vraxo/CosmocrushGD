using Godot;
using System.Threading.Tasks; // Required for Task and async/await

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	private enum SpawnEdge { Top, Right, Bottom, Left }

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene; // Added Tank Enemy scene
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
	private bool isReadyToSpawn = false; // Flag to prevent spawning before ready

	// Make _Ready async
	public override async void _Ready()
	{
		rng.Randomize();
		player = GetNode<Player>(playerPath);

		if (player is null)
		{
			GD.PrintErr("EnemySpawner: Player node not found! Spawner disabled.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}
		if (enemyPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: EnemyPoolManager reference not set! Spawner disabled.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null || tankEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: One or more enemy scenes are not assigned! Spawner may malfunction.");
			// Decide if you want to disable it completely or just run with missing types
		}

		// Wait briefly for other nodes (like EnemyPoolManager) to potentially finish their _Ready
		await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);

		isReadyToSpawn = true; // Mark as ready
		SetupTimers();
		SpawnInitialEnemies();
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
			GD.PrintErr("EnemySpawner: Timer references not set!");
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
		if (!isReadyToSpawn)
		{
			GD.Print("EnemySpawner: Deferring initial spawn, not ready yet.");
			return;
		}

		for (int i = 0; i < initialSpawnCount; i++)
		{
			SpawnRandomEnemy();
		}
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
		if (!isReadyToSpawn)
		{
			return;
		}
		SpawnRandomEnemy();
	}

	private void SpawnRandomEnemy()
	{
		if (!isReadyToSpawn || player is null || enemyPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: Cannot spawn, prerequisites missing or not ready!");
			return;
		}

		PackedScene selectedScene = SelectRandomEnemyScene();
		if (selectedScene is null)
		{
			GD.PrintErr("EnemySpawner: No valid enemy scene selected to spawn.");
			return;
		}

		TrySpawnEnemyFromPool(selectedScene);
	}

	private PackedScene SelectRandomEnemyScene()
	{
		// Update range to include the new TankEnemy type
		int enemyType = rng.RandiRange(0, 3);

		return enemyType switch
		{
			0 => meleeEnemyScene,
			1 => rangedEnemyScene,
			2 => explodingEnemyScene,
			3 => tankEnemyScene, // Add case for TankEnemy
			_ => meleeEnemyScene, // Default fallback
		};
	}

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
			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				// ResetState should already be called by the PoolManager or the enemy itself upon retrieval
				// Ensure enemy is properly setup before use
				enemy.ResetState(spawnPosition); // Call ResetState explicitly after getting from pool
			}
			else
			{
				GD.Print($"EnemySpawner: Failed to get enemy of type {enemyScene.ResourcePath} from pool (returned null).");
			}
		}
		else
		{
			GD.Print("EnemySpawner: Failed to find valid spawn position after multiple attempts.");
		}
	}

	private bool IsPositionValid(Vector2 position)
	{
		if (player is null || !IsInstanceValid(player))
		{
			return false;
		}
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance;
	}

	private Vector2 GetRandomEdgePosition()
	{
		SpawnEdge edge = (SpawnEdge)rng.RandiRange(0, 3);

		float x = 0f;
		float y = 0f;

		switch (edge)
		{
			case SpawnEdge.Top:
				x = rng.RandfRange(spawnMargin.X, worldSize.X - spawnMargin.X);
				y = rng.RandfRange(0, spawnMargin.Y);
				break;
			case SpawnEdge.Right:
				x = rng.RandfRange(worldSize.X - spawnMargin.X, worldSize.X);
				y = rng.RandfRange(spawnMargin.Y, worldSize.Y - spawnMargin.Y);
				break;
			case SpawnEdge.Bottom:
				x = rng.RandfRange(spawnMargin.X, worldSize.X - spawnMargin.X);
				y = rng.RandfRange(worldSize.Y - spawnMargin.Y, worldSize.Y);
				break;
			case SpawnEdge.Left:
				x = rng.RandfRange(0, spawnMargin.X);
				y = rng.RandfRange(spawnMargin.Y, worldSize.Y - spawnMargin.Y);
				break;
		}

		x = Mathf.Clamp(x, 0, worldSize.X);
		y = Mathf.Clamp(y, 0, worldSize.Y);

		return new Vector2(x, y);
	}
}
