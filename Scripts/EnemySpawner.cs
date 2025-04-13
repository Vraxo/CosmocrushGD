using Godot;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private Area2D spawnArea;
	[Export] private NodePath playerPath;
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f; // Adjust this value if needed!
	[Export] private EnemyPoolManager enemyPoolManager;
	[Export] private int initialSpawnCount = 3;

	private Player player;
	private float timeElapsed;
	private const int MaxSpawnAttempts = 10; // Increased attempts slightly for debugging
	private readonly RandomNumberGenerator rng = new();
	private Rect2 spawnRectGlobal;
	private EnemyPoolManager _cachedPoolManager;

	public override void _Ready()
	{
		GD.Print("--- EnemySpawner _Ready: Start ---");
		rng.Randomize();
		player = GetNode<Player>(playerPath);

		if (player is null)
		{
			GD.PrintErr("EnemySpawner: Player node not found! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		if (enemyPoolManager is null || !IsInstanceValid(enemyPoolManager))
		{
			GD.PrintErr("EnemySpawner: EnemyPoolManager reference not set or invalid in Inspector! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}
		_cachedPoolManager = enemyPoolManager;
		GD.Print($"EnemySpawner: Cached Pool Manager reference: {_cachedPoolManager.GetPath()}");

		if (spawnArea is null)
		{
			GD.PrintErr("EnemySpawner: Spawn Area reference not set! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		CalculateSpawnRect();

		if (spawnRectGlobal == default)
		{
			GD.PrintErr("EnemySpawner: Spawn Rect calculation failed. Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		SetupTimers();
		CallDeferred(nameof(SpawnInitialEnemies));
		GD.Print("--- EnemySpawner _Ready: End ---");
	}

	private void CalculateSpawnRect()
	{
		if (spawnArea is null || !IsInstanceValid(spawnArea))
		{
			GD.PrintErr("EnemySpawner: Cannot calculate spawn rect, spawnArea is null or invalid.");
			spawnRectGlobal = default;
			return;
		}

		// Use CollisionShape child
		CollisionShape2D shape = null;
		foreach (Node child in spawnArea.GetChildren())
		{
			if (child is CollisionShape2D cs)
			{
				shape = cs;
				break;
			}
		}

		if (shape is null)
		{
			GD.PrintErr("EnemySpawner: Spawn Area does not have a CollisionShape2D child!");
			spawnRectGlobal = default;
			return;
		}


		if (shape.Shape is not RectangleShape2D rectShape)
		{
			GD.PrintErr($"EnemySpawner: Spawn Area's CollisionShape2D ('{shape.Name}') does not have a RectangleShape2D!");
			spawnRectGlobal = default;
			return;
		}

		GD.Print($"EnemySpawner: Calculating Spawn Rect (Simplified Method)...");
		GD.Print($"  CollisionShape GlobalPosition: {shape.GlobalPosition}");
		GD.Print($"  CollisionShape GlobalScale: {shape.GlobalScale}");
		GD.Print($"  RectangleShape Size: {rectShape.Size}");

		Vector2 globalShapeCenter = shape.GlobalPosition;
		Vector2 scaledRectSize = rectShape.Size * shape.GlobalScale;
		Vector2 topLeft = globalShapeCenter - (scaledRectSize / 2f);

		spawnRectGlobal = new Rect2(topLeft, scaledRectSize);

		GD.Print($"EnemySpawner: Spawn Rect calculated: Position={spawnRectGlobal.Position}, Size={spawnRectGlobal.Size}, End={spawnRectGlobal.End}");
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
		GD.Print("EnemySpawner: Running Deferred SpawnInitialEnemies.");
		if (_cachedPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: Cached pool manager is null in SpawnInitialEnemies (Deferred). Cannot spawn.");
			return;
		}
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: Cannot perform initial spawn, one or more enemy scenes are not assigned.");
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
		if (_cachedPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: OnSpawnTimerTimeout: Cached pool manager is null. Cannot spawn.");
			return;
		}
		SpawnRandomEnemy();
	}

	private void SpawnRandomEnemy()
	{
		if (meleeEnemyScene is null || rangedEnemyScene is null || explodingEnemyScene is null || player is null || _cachedPoolManager is null || spawnArea is null)
		{
			GD.PrintErr($"EnemySpawner: Missing required references for SpawnRandomEnemy! Player: {player}, PoolManager: {_cachedPoolManager}, SpawnArea: {spawnArea}, Melee: {meleeEnemyScene}, Ranged: {rangedEnemyScene}, Exploding: {explodingEnemyScene}");
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
		float requiredDistSq = minPlayerDistance * minPlayerDistance; // Cache calculation

		if (spawnRectGlobal == default)
		{
			GD.PrintErr($"EnemySpawner: TrySpawnEnemyFromPool: spawnRectGlobal is invalid. Cannot generate spawn position.");
			return;
		}
		if (player is null || !IsInstanceValid(player)) // Check player validity early
		{
			GD.PrintErr($"EnemySpawner: TrySpawnEnemyFromPool: Player is invalid. Cannot check position validity.");
			return;
		}


		while (!foundValidPosition && attempts < MaxSpawnAttempts)
		{
			spawnPosition = GetRandomPositionInArea();
			float distSq = player.GlobalPosition.DistanceSquaredTo(spawnPosition);
			bool positionOk = distSq >= requiredDistSq;

			// --- DETAILED LOGGING INSIDE LOOP ---
			GD.Print($"  Attempt {attempts + 1}: Pos={spawnPosition}, PlayerPos={player.GlobalPosition}, DistSq={distSq:F2}, RequiredDistSq={requiredDistSq:F2}, Valid={positionOk}");
			// --- END LOGGING ---

			if (positionOk)
			{
				foundValidPosition = true;
			}

			attempts++;
		}

		if (foundValidPosition)
		{
			if (_cachedPoolManager is null)
			{
				GD.PrintErr($"EnemySpawner: TrySpawnEnemyFromPool: _cachedPoolManager became null unexpectedly!");
				return;
			}

			GD.Print($"EnemySpawner: Attempting to get enemy '{enemyScene.ResourcePath}' from PoolManager: {_cachedPoolManager.GetPath()}");
			BaseEnemy enemy = _cachedPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				GD.Print($"EnemySpawner: Spawning {enemy.Name} at {spawnPosition}");
				enemy.ResetState(spawnPosition);
			}
			else
			{
				GD.PrintErr($"EnemySpawner: _cachedPoolManager.GetEnemy returned NULL for scene '{enemyScene.ResourcePath}'.");
			}
		}
		else
		{
			GD.PrintErr("EnemySpawner: Failed to find valid spawn position after multiple attempts."); // Changed to Error for visibility
		}
	}

	// --- IsPositionValid removed as logic moved into the loop ---
	// private bool IsPositionValid(Vector2 position) { ... }


	private Vector2 GetRandomPositionInArea()
	{
		// spawnRectGlobal is calculated in _Ready
		float randomX = rng.RandfRange(spawnRectGlobal.Position.X, spawnRectGlobal.End.X);
		float randomY = rng.RandfRange(spawnRectGlobal.Position.Y, spawnRectGlobal.End.Y);
		Vector2 randomPosition = new(randomX, randomY);
		return randomPosition;
	}
}
