using Godot;
using System.Linq;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	// Removed SpawnEdge enum

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene;
	// Removed spawnMargin export
	[Export] private NodePath playerPath;
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;

	[Export] private NodePath spawnAreaNodePath;

	[Export] private EnemyPoolManager enemyPoolManager;
	[Export] private int initialSpawnCount = 3;

	private Player player;
	private float timeElapsed;
	private Rect2 _spawnAreaRect; // Stores the calculated spawn area bounds
	private Area2D _spawnAreaNode;
	private const int MaxSpawnAttempts = 10; // Renamed from MaxSpawnAttempts for clarity in this context
	private readonly RandomNumberGenerator rng = new();
	// Removed InwardOffsetMultiplier

	public override void _Ready()
	{
		rng.Randomize();

		if (playerPath is not null)
		{
			player = GetNode<Player>(playerPath);
		}
		if (player is null)
		{
			GD.PrintErr("EnemySpawner: Player node not found! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}
		if (enemyPoolManager is null)
		{
			GD.PrintErr("EnemySpawner: EnemyPoolManager reference not set in Inspector! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		if (spawnAreaNodePath is not null)
		{
			_spawnAreaNode = GetNode<Area2D>(spawnAreaNodePath);
			if (_spawnAreaNode is not null)
			{
				var collisionShape = _spawnAreaNode.GetChildren().OfType<CollisionShape2D>().FirstOrDefault();
				if (collisionShape is not null && collisionShape.Shape is RectangleShape2D rectShape)
				{
					Vector2 shapeSize = rectShape.Size * _spawnAreaNode.Scale;
					Vector2 extents = shapeSize / 2.0f;
					Vector2 globalCenter = _spawnAreaNode.GlobalPosition;
					_spawnAreaRect = new Rect2(globalCenter - extents, shapeSize);
					GD.Print($"EnemySpawner: Calculated Spawn Area Rect: Position={_spawnAreaRect.Position}, Size={_spawnAreaRect.Size}");

					if (_spawnAreaRect.Size.X <= 1 || _spawnAreaRect.Size.Y <= 1)
					{
						GD.PrintErr($"EnemySpawner: Calculated spawn area has minimal or negative size ({_spawnAreaRect.Size}). Check Area2D scale and shape size. Spawner disabled.");
						SetProcess(false); SetPhysicsProcess(false); return;
					}
				}
				else
				{
					GD.PrintErr($"EnemySpawner: Assigned Area2D '{_spawnAreaNode.Name}' does not have a valid RectangleShape2D child. Spawner disabled.");
					SetProcess(false); SetPhysicsProcess(false); return;
				}
			}
			else
			{
				GD.PrintErr($"EnemySpawner: Could not find Area2D node at path: {spawnAreaNodePath}. Spawner disabled.");
				SetProcess(false); SetPhysicsProcess(false); return;
			}
		}
		else
		{
			GD.PrintErr("EnemySpawner: Spawn Area Node Path not assigned in Inspector! Spawner disabled.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		SetupTimers();
		CallDeferred(nameof(SpawnInitialEnemies));
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
			GD.PrintErr("EnemySpawner: Timer references not set! Make sure Timers are assigned in the Inspector.");
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		spawnTimer.Timeout += OnSpawnTimerTimeout;
		rateIncreaseTimer.Timeout += RateIncrease;
		spawnTimer.WaitTime = CalculateSpawnInterval();
		spawnTimer.Start();
		rateIncreaseTimer.Start();
	}

	private void SpawnInitialEnemies()
	{
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: No enemy scenes assigned. Cannot perform initial spawn.");
			return;
		}

		GD.Print("EnemySpawner: Starting initial enemy spawn...");
		int spawnedCount = 0;
		for (int i = 0; i < initialSpawnCount; i++)
		{
			if (SpawnRandomEnemy())
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

	private bool SpawnRandomEnemy()
	{
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null)
		{
			GD.PrintErr("EnemySpawner: Cannot spawn random enemy, no scenes are assigned.");
			return false;
		}

		PackedScene selectedScene = SelectRandomEnemyScene();
		if (selectedScene is not null)
		{
			TrySpawnEnemyFromPool(selectedScene);
			return true;
		}
		else
		{
			GD.PrintErr("EnemySpawner: SelectRandomEnemyScene returned null. This shouldn't happen if at least one scene is assigned.");
			return false;
		}
	}

	private PackedScene SelectRandomEnemyScene()
	{
		var availableScenes = new Godot.Collections.Array<PackedScene>();
		if (meleeEnemyScene is not null) availableScenes.Add(meleeEnemyScene);
		if (rangedEnemyScene is not null) availableScenes.Add(rangedEnemyScene);
		if (explodingEnemyScene is not null) availableScenes.Add(explodingEnemyScene);
		if (tankEnemyScene is not null) availableScenes.Add(tankEnemyScene);

		if (availableScenes.Count == 0)
		{
			return null;
		}

		int randomIndex = rng.RandiRange(0, availableScenes.Count - 1);
		return availableScenes[randomIndex];
	}

	// Modified to use GetRandomPointInArea
	private void TrySpawnEnemyFromPool(PackedScene enemyScene)
	{
		bool foundValidPosition = false;
		int attempts = 0;
		Vector2 spawnPosition = Vector2.Zero;

		while (!foundValidPosition && attempts < MaxSpawnAttempts)
		{
			// Get a random point anywhere *inside* the defined rectangle
			spawnPosition = GetRandomPointInArea();

			// Check only against player distance
			if (IsPositionValid(spawnPosition))
			{
				foundValidPosition = true;
			}
			attempts++;
		}

		if (foundValidPosition)
		{
			if (enemyPoolManager is null || !IsInstanceValid(enemyPoolManager))
			{
				GD.PrintErr("EnemySpawner: EnemyPoolManager became invalid before spawning. Aborting spawn.");
				return;
			}

			BaseEnemy enemy = enemyPoolManager.GetEnemy(enemyScene);

			if (enemy is not null)
			{
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
				GD.Print($"EnemySpawner: Failed to get enemy of type {enemyScene.ResourcePath} from pool (PoolManager returned null).");
			}
		}
		else
		{
			// If it fails, it's likely because all random points were too close to the player
			GD.Print($"EnemySpawner: Failed to find valid spawn position (far enough from player) within Area2D for {enemyScene.ResourcePath} after {MaxSpawnAttempts} attempts.");
		}
	}

	// Checks if the position is far enough from the player
	private bool IsPositionValid(Vector2 position)
	{
		if (player is null || !IsInstanceValid(player))
		{
			return false; // Cannot validate if player doesn't exist
		}
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance;
	}

	// --- NEW: Generates a random point within the _spawnAreaRect ---
	private Vector2 GetRandomPointInArea()
	{
		float randomX = rng.RandfRange(_spawnAreaRect.Position.X, _spawnAreaRect.End.X);
		float randomY = rng.RandfRange(_spawnAreaRect.Position.Y, _spawnAreaRect.End.Y);
		return new Vector2(randomX, randomY);
	}

	// Removed GetRandomEdgePosition function entirely
}
