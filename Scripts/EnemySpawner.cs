using Godot;
using System.Linq;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	[Signal]
	public delegate void EnemySpawnedEventHandler(BaseEnemy enemy);

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene; // Keep for checking availability
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene;
	[Export] private PackedScene swiftEnemyScene;
	[Export] private NodePath playerPath;
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;
	[Export] private NodePath spawnAreaNodePath;
	[Export] private int maxInitialSpawns = 3;
	[Export] private float spawnRateMultiplier = 10.0f; // Temporary multiplier

	private Player player;
	private float timeElapsed;
	private Rect2 _spawnAreaRect;
	private Area2D _spawnAreaNode;
	private const int MaxSpawnAttempts = 10;
	private readonly RandomNumberGenerator rng = new();
	private readonly Godot.Collections.Array<PackedScene> _sceneSelectionCache = new(); // Cache for selection

	public override void _Ready()
	{
		rng.Randomize();

		if (playerPath is not null)
		{
			player = GetNode<Player>(playerPath);
		}
		if (player is null)
		{
			GD.PrintErr("EnemySpawner: Player not found. Disabling spawner.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		if (spawnAreaNodePath is not null)
		{
			_spawnAreaNode = GetNode<Area2D>(spawnAreaNodePath);
			if (_spawnAreaNode is not null)
			{
				var collisionShape = _spawnAreaNode.GetChildren().OfType<CollisionShape2D>().FirstOrDefault();
				if (collisionShape?.Shape is RectangleShape2D rectShape)
				{
					Vector2 shapeSize = rectShape.Size * _spawnAreaNode.Scale;
					Vector2 extents = shapeSize / 2.0f;
					Vector2 globalCenter = _spawnAreaNode.GlobalPosition;
					_spawnAreaRect = new Rect2(globalCenter - extents, shapeSize);

					if (_spawnAreaRect.Size.X <= 1 || _spawnAreaRect.Size.Y <= 1)
					{
						GD.PrintErr("EnemySpawner: Spawn area size is invalid. Disabling spawner.");
						SetProcess(false);
						SetPhysicsProcess(false);
						return;
					}
				}
				else
				{
					GD.PrintErr("EnemySpawner: Spawn area CollisionShape2D or RectangleShape2D not found/invalid. Disabling spawner.");
					SetProcess(false);
					SetPhysicsProcess(false);
					return;
				}
			}
			else
			{
				GD.PrintErr("EnemySpawner: Spawn area Area2D node not found. Disabling spawner.");
				SetProcess(false);
				SetPhysicsProcess(false);
				return;
			}
		}
		else
		{
			GD.PrintErr("EnemySpawner: Spawn area node path not set. Disabling spawner.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		SetupTimers();
		CallDeferred(nameof(SpawnInitialEnemies));
		EnemyPoolManager.Instance?.RegisterSpawner(this);
	}

	public override void _ExitTree()
	{
		EnemyPoolManager.Instance?.UnregisterSpawner(this);
		base._ExitTree();
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
			GD.PrintErr("EnemySpawner: One or more timers are null. Disabling spawner.");
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
		if (EnemyPoolManager.Instance is null)
		{
			GD.PrintErr("EnemySpawner: EnemyPoolManager not ready for initial spawn.");
			return;
		}

		int spawnedCount = 0;
		for (int i = 0; i < maxInitialSpawns; i++)
		{
			if (SpawnRandomEnemy())
			{
				spawnedCount++;
			}
		}
		GD.Print($"EnemySpawner: Spawned {spawnedCount} initial enemies.");
	}

	private void RateIncrease()
	{
		timeElapsed += (float)rateIncreaseTimer.WaitTime;
		spawnTimer.WaitTime = CalculateSpawnInterval();
	}

	private float CalculateSpawnInterval()
	{
		float effectiveMultiplier = Mathf.Max(0.001f, spawnRateMultiplier);

		// Calculate the interval based on time progression and then apply the multiplier
		float timeAdjustedBaseInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
		float intervalToClamp = timeAdjustedBaseInterval / effectiveMultiplier;

		// Determine dynamic clamping bounds based on the multiplier
		float finalMinInterval;
		float finalMaxInterval;

		if (effectiveMultiplier > 1.0f) // Speeding up
		{
			finalMinInterval = Mathf.Max(0.01f, minSpawnInterval / effectiveMultiplier); // Allow faster minimum
			finalMaxInterval = baseSpawnRate; // Cap at original base (slowest) rate, effectively it will hit finalMinInterval much sooner
		}
		else if (effectiveMultiplier < 1.0f) // Slowing down
		{
			finalMinInterval = minSpawnInterval; // Minimum is still original minimum
			finalMaxInterval = baseSpawnRate / effectiveMultiplier; // Allow slower maximum
		}
		else // Normal rate (multiplier is 1.0)
		{
			finalMinInterval = minSpawnInterval;
			finalMaxInterval = baseSpawnRate;
		}

		return float.Clamp(intervalToClamp, finalMinInterval, finalMaxInterval);
	}

	private void OnSpawnTimerTimeout()
	{
		SpawnRandomEnemy();
	}

	private bool SpawnRandomEnemy()
	{
		if (EnemyPoolManager.Instance is null)
		{
			GD.PrintErr("EnemySpawner: EnemyPoolManager not ready for spawn.");
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
			GD.Print("EnemySpawner: No available enemy scenes to spawn.");
			return false;
		}
	}

	private PackedScene SelectRandomEnemyScene()
	{
		_sceneSelectionCache.Clear(); // Use cached list to avoid allocations
		if (meleeEnemyScene is not null) _sceneSelectionCache.Add(meleeEnemyScene);
		if (rangedEnemyScene is not null) _sceneSelectionCache.Add(rangedEnemyScene);
		if (explodingEnemyScene is not null) _sceneSelectionCache.Add(explodingEnemyScene);
		if (tankEnemyScene is not null) _sceneSelectionCache.Add(tankEnemyScene);
		if (swiftEnemyScene is not null) _sceneSelectionCache.Add(swiftEnemyScene);

		if (_sceneSelectionCache.Count == 0)
		{
			return null;
		}

		int randomIndex = rng.RandiRange(0, _sceneSelectionCache.Count - 1);
		return _sceneSelectionCache[randomIndex];
	}

	private void TrySpawnEnemyFromPool(PackedScene enemyScene)
	{
		bool foundValidPosition = false;
		int attempts = 0;
		Vector2 spawnPosition = Vector2.Zero;

		while (!foundValidPosition && attempts < MaxSpawnAttempts)
		{
			spawnPosition = GetRandomPointInArea();

			if (IsPositionValid(spawnPosition))
			{
				foundValidPosition = true;
			}
			attempts++;
		}

		if (foundValidPosition)
		{
			BaseEnemy enemy = EnemyPoolManager.Instance.GetEnemy(enemyScene);

			if (enemy is not null)
			{
				// Reparent the enemy from the pool manager to this spawner (or another container)
				enemy.GetParent()?.RemoveChild(enemy);
				AddChild(enemy); // Add as a child of the spawner

				enemy.ResetAndActivate(spawnPosition, player); // New method to handle activation logic
				EmitSignal(SignalName.EnemySpawned, enemy);
			}
			else
			{
				GD.PrintErr($"EnemySpawner: Failed to get enemy of type {enemyScene.ResourcePath} from pool.");
			}
		}
		else
		{
			GD.Print($"EnemySpawner: Could not find valid spawn position for {enemyScene.ResourcePath} after {MaxSpawnAttempts} attempts.");
		}
	}

	private bool IsPositionValid(Vector2 position)
	{
		if (player is null || !IsInstanceValid(player))
		{
			return false; // Cannot validate if player is gone
		}
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance;
	}

	private Vector2 GetRandomPointInArea()
	{
		float randomX = rng.RandfRange(_spawnAreaRect.Position.X, _spawnAreaRect.End.X);
		float randomY = rng.RandfRange(_spawnAreaRect.Position.Y, _spawnAreaRect.End.Y);
		return new Vector2(randomX, randomY);
	}
}
