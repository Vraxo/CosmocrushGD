using Godot;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	private enum SpawnEdge { Top, Right, Bottom, Left }

	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private Vector2 spawnMargin = new(100, 100);
	[Export] private NodePath playerPath;
	[Export] private NodePath enemyContainerPath;
	[Export] private float baseSpawnRate = 2.0f;
	[Export] private float minSpawnInterval = 0.5f;
	[Export] private float timeMultiplier = 0.1f;
	[Export] private float minPlayerDistance = 500.0f;
	[Export] private Vector2 worldSize = new(2272, 1208);
	[Export] private Timer spawnTimer;
	[Export] private Timer rateIncreaseTimer;

	private Player player;
	private bool spawnMeleeNext = true;
	private float timeElapsed;
	private const int MaxSpawnAttempts = 10;

	public override void _Ready()
	{
		player = GetNode<Player>(playerPath);

		if (player is null)
		{
			GD.PrintErr("Player node not found!");
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

		spawnTimer.Timeout += SpawnEnemy;
		rateIncreaseTimer.Timeout += RateIncrease;
	}

	private void RateIncrease()
	{
		timeElapsed += 1.0f;
		spawnTimer.WaitTime = CalculateSpawnInterval();
		GD.Print($"Current spawn interval: {spawnTimer.WaitTime:0.00}");
	}

	private float CalculateSpawnInterval()
	{
		float calculatedInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
		return Mathf.Clamp(calculatedInterval, minSpawnInterval, baseSpawnRate);
	}

	private void SpawnEnemy()
	{
		if (meleeEnemyScene is null || rangedEnemyScene is null || player is null)
		{
			GD.PrintErr("Missing required references!");
			return;
		}

		var selectedScene = spawnMeleeNext ? meleeEnemyScene : rangedEnemyScene;
		spawnMeleeNext = !spawnMeleeNext;

		TrySpawnEnemy(selectedScene);
	}

	private void TrySpawnEnemy(PackedScene enemyScene)
	{
		bool foundValidPosition = false;
		int attempts = 0;

		while (!foundValidPosition && attempts < MaxSpawnAttempts)
		{
			Vector2 spawnPosition = GetRandomEdgePosition();

			if (IsPositionValid(spawnPosition))
			{
				CreateEnemy(enemyScene, spawnPosition);
				foundValidPosition = true;
			}

			attempts++;
		}

		if (!foundValidPosition)
		{
			GD.Print("Failed to find valid spawn position");
		}
	}

	private bool IsPositionValid(Vector2 position)
	{
		return position.DistanceTo(player.GlobalPosition) >= minPlayerDistance;
	}

	private Vector2 GetRandomEdgePosition()
	{
		SpawnEdge edge = (SpawnEdge)(GD.Randi() % 4);
		float x = 0f;
		float y = 0f;

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
		}

		return new Vector2(x, y);
	}

	private void CreateEnemy(PackedScene scene, Vector2 position)
	{
		var enemy = scene.Instantiate<Node2D>();
		enemy.Position = position;
		AddChild(enemy);
	}
}
