using Godot;
using System.Linq;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	[Signal]
	public delegate void EnemySpawnedEventHandler(BaseEnemy enemy);

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

	private Player player;
	private float timeElapsed;
	private Rect2 _spawnAreaRect;
	private Area2D _spawnAreaNode;
	private const int MaxSpawnAttempts = 10;
	private readonly RandomNumberGenerator rng = new();

	public override void _Ready()
	{
		rng.Randomize();

		if (playerPath is not null)
		{
			player = GetNode<Player>(playerPath);
		}
		if (player is null)
		{
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


					if (_spawnAreaRect.Size.X <= 1 || _spawnAreaRect.Size.Y <= 1)
					{
						SetProcess(false); SetPhysicsProcess(false); return;
					}
				}
				else
				{
					SetProcess(false); SetPhysicsProcess(false); return;
				}
			}
			else
			{
				SetProcess(false); SetPhysicsProcess(false); return;
			}
		}
		else
		{
			SetProcess(false); SetPhysicsProcess(false); return;
		}

		SetupTimers();
		CallDeferred(nameof(SpawnInitialEnemies));
	}

	private void SetupTimers()
	{
		if (spawnTimer is null || rateIncreaseTimer is null)
		{
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
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null && swiftEnemyScene is null)
		{
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
		if (meleeEnemyScene is null && rangedEnemyScene is null && explodingEnemyScene is null && tankEnemyScene is null && swiftEnemyScene is null)
		{
			return false;
		}

		PackedScene selectedScene = SelectRandomEnemyScene();
		if (selectedScene is not null)
		{
			TrySpawnEnemy(selectedScene);
			return true;
		}
		else
		{
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
		if (swiftEnemyScene is not null) availableScenes.Add(swiftEnemyScene);

		if (availableScenes.Count == 0)
		{
			return null;
		}

		int randomIndex = rng.RandiRange(0, availableScenes.Count - 1);
		return availableScenes[randomIndex];
	}

	private void TrySpawnEnemy(PackedScene enemyScene)
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
			BaseEnemy enemy = enemyScene.Instantiate<BaseEnemy>();

			if (enemy is not null)
			{
				enemy.GlobalPosition = spawnPosition;
				AddChild(enemy);
				EmitSignal(SignalName.EnemySpawned, enemy);
			}
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
		float randomX = rng.RandfRange(_spawnAreaRect.Position.X, _spawnAreaRect.End.X);
		float randomY = rng.RandfRange(_spawnAreaRect.Position.Y, _spawnAreaRect.End.Y);
		return new Vector2(randomX, randomY);
	}
}
