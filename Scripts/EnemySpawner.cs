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

		SetupTimers();
		// Defer initial spawning to ensure pool manager is ready
		CallDeferred(MethodName.SpawnInitialEnemies);
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
		GD.Print("EnemySpawner: Starting initial spawn..."); // Added log
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: Cannot perform initial spawn, one or more enemy scenes are not assigned.");
			return;
		}

		if (enemyPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: Cannot perform initial spawn, EnemyPoolManager reference is null.");
			return;
		}


		for (int i = 0; i < initialSpawnCount; i++)
		{
			SpawnRandomEnemy();
		}
		GD.Print("EnemySpawner: Finished initial spawn."); // Added log
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

	private void SpawnRandomEnemy()
	{
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null || player is null || enemyPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: Missing required references for spawning!");
			return;
		}

		PackedScene selectedScene = SelectRandomEnemyScene();
		TrySpawnEnemyFromPool(selectedScene);
	}

	private PackedScene SelectRandomEnemyScene()
	{
		int enemyType = rng.RandiRange(0, 2);

		return enemyType switch
		{
			0 => meleeEnemyScene,
			1 => rangedEnemyScene,
			_ => explodingEnemyScene,
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
			// GD.Print($"Attempting to get enemy from pool: {enemyScene?.ResourcePath ?? "NULL SCENE"}"); // Added log
			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				enemy.ResetState(spawnPosition);
			}
			else
			{
				GD.Print($"EnemySpawner: Failed to get enemy of type {enemyScene?.ResourcePath ?? "NULL SCENE"} from pool.");
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
