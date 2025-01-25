using Godot;

public partial class EnemySpawner : Node
{
    private PackedScene enemyScene; // Drag and drop your enemy scene here in the editor
    [Export] private float spawnRate = 2.0f; // Enemies will spawn every 2 seconds
    [Export] private Vector2 spawnMargin = new Vector2(50, 50); // Margin outside the screen where enemies spawn

    private Timer spawnTimer;
    private Viewport viewport;

    public override void _Ready()
    {
        viewport = GetViewport();
        SetupTimer();

        enemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
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
        if (enemyScene == null)
        {
            GD.PrintErr("Enemy scene is not assigned!");
            return;
        }

        Rect2 screenBounds = new(viewport.GetVisibleRect().Position, viewport.GetVisibleRect().Size);
        Vector2 spawnPosition = GetRandomSpawnPosition(screenBounds);

        var enemy = enemyScene.Instantiate<Node2D>();
        enemy.Position = spawnPosition;

        // Add the enemy to the scene
        GetParent().AddChild(enemy);
    }

    private Vector2 GetRandomSpawnPosition(Rect2 screenBounds)
    {
        // Choose a random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = (int)(GD.Randi() % 4); // Explicit cast to int

        float x, y;
        switch (edge)
        {
            case 0: // Top
                x = (float)GD.RandRange(screenBounds.Position.X - spawnMargin.X, screenBounds.End.X + spawnMargin.X); // Explicit cast to float
                y = screenBounds.Position.Y - spawnMargin.Y;
                break;
            case 1: // Right
                x = screenBounds.End.X + spawnMargin.X;
                y = (float)GD.RandRange(screenBounds.Position.Y - spawnMargin.Y, screenBounds.End.Y + spawnMargin.Y); // Explicit cast to float
                break;
            case 2: // Bottom
                x = (float)GD.RandRange(screenBounds.Position.X - spawnMargin.X, screenBounds.End.X + spawnMargin.X); // Explicit cast to float
                y = screenBounds.End.Y + spawnMargin.Y;
                break;
            default: // Left
                x = screenBounds.Position.X - spawnMargin.X;
                y = (float)GD.RandRange(screenBounds.Position.Y - spawnMargin.Y, screenBounds.End.Y + spawnMargin.Y); // Explicit cast to float
                break;
        }

        return new Vector2(x, y);
    }
}
