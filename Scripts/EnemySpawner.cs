using Godot;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
	[Export] private PackedScene enemyScene;
	[Export] private float spawnRate = 2.0f;
	[Export] private Vector2 spawnMargin = new(50, 50);

	private Timer spawnTimer;
	private Viewport viewport;

	public override void _Ready()
	{
		viewport = GetViewport();
		SetupTimer();
	}

	private void SetupTimer()
	{
		spawnTimer = new Timer
		{
			WaitTime = spawnRate,
			Autostart = true,
			OneShot = false
		};

		spawnTimer.Timeout += SpawnEnemy;
		AddChild(spawnTimer);
		spawnTimer.Start();
	}

	private void SpawnEnemy()
	{
		if (enemyScene is null)
		{
			GD.PrintErr("Enemy scene is not assigned!");
			return;
		}

		Rect2 visibleRect = viewport.GetVisibleRect();
		Rect2 screenBounds = new(visibleRect.Position, visibleRect.Size);
		Vector2 spawnPosition = GetRandomSpawnPosition(screenBounds);

		var enemy = enemyScene.Instantiate<Node2D>();
		enemy.Position = spawnPosition;

		GetParent().AddChild(enemy);
	}

	private Vector2 GetRandomSpawnPosition(Rect2 screenBounds)
	{
		var edge = (int)(GD.Randi() % 4);

		float x;
		float y;

		switch (edge)
		{
			case 0:
				x = (float)GD.RandRange(screenBounds.Position.X - spawnMargin.X, screenBounds.End.X + spawnMargin.X);
				y = screenBounds.Position.Y - spawnMargin.Y;
				break;
			case 1:
				x = screenBounds.End.X + spawnMargin.X;
				y = (float)GD.RandRange(screenBounds.Position.Y - spawnMargin.Y, screenBounds.End.Y + spawnMargin.Y);
				break;
			case 2:
				x = (float)GD.RandRange(screenBounds.Position.X - spawnMargin.X, screenBounds.End.X + spawnMargin.X);
				y = screenBounds.End.Y + spawnMargin.Y;
				break;
			default:
				x = screenBounds.Position.X - spawnMargin.X;
				y = (float)GD.RandRange(screenBounds.Position.Y - spawnMargin.Y, screenBounds.End.Y + spawnMargin.Y);
				break;
		}

		return new Vector2(x, y);
	}
}
