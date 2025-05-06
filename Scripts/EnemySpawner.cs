using Godot;
using System.Linq;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	[Signal]
	public delegate void EnemySpawnedEventHandler(BaseEnemy enemy);

	private const int MaxSpawnAttempts = 10;
	private const int MaxAllowedEnemies = 150;

	private readonly RandomNumberGenerator rng = new();
	private readonly Godot.Collections.Array<PackedScene> _sceneSelectionCache = new();

	private Player player;
	private float timeElapsed;
	private Rect2 _spawnAreaRect;
	private Area2D _spawnAreaNode;
	private World world;

	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;
	[Export] private PackedScene meleeEnemyScene;
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

	public override void _Ready()
	{
		rng.Randomize();

		world = GetNode<World>("/root/World");
		if (world is null)
		{
			GD.PrintErr("EnemySpawner: World node not found. Disabling spawner.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

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
					var shapeSize = rectShape.Size * _spawnAreaNode.Scale;
					var extents = shapeSize / 2.0f;
					var globalCenter = _spawnAreaNode.GlobalPosition;
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
		var spawnedCount = 0;
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
		var calculatedInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
		return float.Clamp(calculatedInterval, minSpawnInterval, baseSpawnRate);
	}

	private void OnSpawnTimerTimeout()
	{
		SpawnRandomEnemy();
	}

	private bool SpawnRandomEnemy()
	{
		if (world is null)
		{
			GD.PrintErr("EnemySpawner: World instance not available to check enemy count.");
			return false;
		}

		if (world.CurrentEnemyCount >= MaxAllowedEnemies)
		{
			return false;
		}

		var selectedScene = SelectRandomEnemyScene();
		if (selectedScene is not null)
		{
			TrySpawnEnemy(selectedScene);
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
		_sceneSelectionCache.Clear();
		if (meleeEnemyScene is not null) _sceneSelectionCache.Add(meleeEnemyScene);
		if (rangedEnemyScene is not null) _sceneSelectionCache.Add(rangedEnemyScene);
		if (explodingEnemyScene is not null) _sceneSelectionCache.Add(explodingEnemyScene);
		if (tankEnemyScene is not null) _sceneSelectionCache.Add(tankEnemyScene);
		if (swiftEnemyScene is not null) _sceneSelectionCache.Add(swiftEnemyScene);

		if (_sceneSelectionCache.Count == 0)
		{
			return null;
		}

		var randomIndex = rng.RandiRange(0, _sceneSelectionCache.Count - 1);
		return _sceneSelectionCache[randomIndex];
	}

	private void TrySpawnEnemy(PackedScene enemyScene)
	{
		var foundValidPosition = false;
		var attempts = 0;
		var spawnPosition = Vector2.Zero;

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
			var enemy = enemyScene.Instantiate<BaseEnemy>();

			if (enemy is not null)
			{
				AddChild(enemy);

				enemy.ResetAndActivate(spawnPosition, player);
				EmitSignal(SignalName.EnemySpawned, enemy);
			}
			else
			{
				GD.PrintErr($"EnemySpawner: Failed to instantiate enemy of type {enemyScene.ResourcePath}.");
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
			return false;
		}
		return position.DistanceSquaredTo(player.GlobalPosition) >= minPlayerDistance * minPlayerDistance;
	}

	private Vector2 GetRandomPointInArea()
	{
		var randomX = rng.RandfRange(_spawnAreaRect.Position.X, _spawnAreaRect.End.X);
		var randomY = rng.RandfRange(_spawnAreaRect.Position.Y, _spawnAreaRect.End.Y);
		return new Vector2(randomX, randomY);
	}
}
