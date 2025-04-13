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
	[Export] private NodePath spawnAreaPath; // Path to the Area2D defining spawn bounds
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;
	// [Export] private Vector2 worldSize = new(2272, 1208); // Removed
	[Export] private EnemyPoolManager enemyPoolManager;
	[Export] private int initialSpawnCount = 3;

	private Player player;
	private Rect2 spawnBounds; // Calculated spawn area
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

		if (!CalculateSpawnBounds())
		{
			// Error message printed within CalculateSpawnBounds
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		SetupTimers();
		SpawnInitialEnemies();
	}

	private bool CalculateSpawnBounds()
	{
		if (spawnAreaPath is null)
		{
			GD.PrintErr("EnemySpawner: Spawn Area Path not set!");
			return false;
		}

		var spawnAreaNode = GetNode<Area2D>(spawnAreaPath);
		if (spawnAreaNode is null)
		{
			GD.PrintErr($"EnemySpawner: Could not find Spawn Area node at path: {spawnAreaPath}");
			return false;
		}

		var collisionShape = spawnAreaNode.GetNode<CollisionShape2D>("CollisionShape2D"); // Assumes default name
		if (collisionShape is null)
		{
			GD.PrintErr($"EnemySpawner: Could not find CollisionShape2D child of Spawn Area node: {spawnAreaNode.Name}");
			return false;
		}

		if (collisionShape.Shape is not RectangleShape2D rectangleShape)
		{
			GD.PrintErr($"EnemySpawner: CollisionShape2D in Spawn Area node does not have a RectangleShape2D.");
			return false;
		}

		// Calculate the global Rect based on Area2D position and RectangleShape size
		// Assumes Area2D origin is top-left and no scaling on Area2D or CollisionShape2D
		Vector2 areaSize = rectangleShape.Size;
		Vector2 areaTopLeft = spawnAreaNode.GlobalPosition;
		spawnBounds = new Rect2(areaTopLeft, areaSize);

		GD.Print($"EnemySpawner: Calculated Spawn Bounds: {spawnBounds}");
		return true;
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
			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				enemy.ResetState(spawnPosition);
			}
			else
			{
				GD.Print($"EnemySpawner: Failed to get enemy of type {enemyScene.ResourcePath} from pool.");
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

		// Check distance from player
		if (position.DistanceSquaredTo(player.GlobalPosition) < minPlayerDistance * minPlayerDistance)
		{
			return false;
		}

		// Check if position is within the calculated bounds (sanity check)
		if (!spawnBounds.HasPoint(position))
		{
			GD.Print($"EnemySpawner: Proposed spawn position {position} is outside spawn bounds {spawnBounds}.");
			return false; // Should ideally not happen if GetRandomEdgePosition is correct
		}

		return true;
	}

	private Vector2 GetRandomEdgePosition()
	{
		SpawnEdge edge = (SpawnEdge)rng.RandiRange(0, 3);

		float minX = spawnBounds.Position.X;
		float maxX = spawnBounds.End.X;
		float minY = spawnBounds.Position.Y;
		float maxY = spawnBounds.End.Y;

		float x = 0f;
		float y = 0f;

		switch (edge)
		{
			case SpawnEdge.Top:
				// Spawn along the top edge, inset by margin horizontally, within margin vertically
				x = rng.RandfRange(minX + spawnMargin.X, maxX - spawnMargin.X);
				y = rng.RandfRange(minY, minY + spawnMargin.Y);
				break;
			case SpawnEdge.Right:
				// Spawn along the right edge, within margin horizontally, inset by margin vertically
				x = rng.RandfRange(maxX - spawnMargin.X, maxX);
				y = rng.RandfRange(minY + spawnMargin.Y, maxY - spawnMargin.Y);
				break;
			case SpawnEdge.Bottom:
				// Spawn along the bottom edge, inset by margin horizontally, within margin vertically
				x = rng.RandfRange(minX + spawnMargin.X, maxX - spawnMargin.X);
				y = rng.RandfRange(maxY - spawnMargin.Y, maxY);
				break;
			case SpawnEdge.Left:
				// Spawn along the left edge, within margin horizontally, inset by margin vertically
				x = rng.RandfRange(minX, minX + spawnMargin.X);
				y = rng.RandfRange(minY + spawnMargin.Y, maxY - spawnMargin.Y);
				break;
		}

		// Clamp values to ensure they stay strictly within the spawnBounds, even with margins.
		x = Mathf.Clamp(x, minX, maxX);
		y = Mathf.Clamp(y, minY, maxY);

		return new Vector2(x, y);
	}
}
