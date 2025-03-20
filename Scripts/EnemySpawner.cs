using Godot;

namespace CosmocrushGD;

public partial class EnemySpawner : Node
{
    [Export] private PackedScene meleeEnemyScene;
    [Export] private PackedScene rangedEnemyScene;
    [Export] private float baseSpawnRate = 2.0f;
    [Export] private Vector2 spawnMargin = new(100, 100);
    [Export] private NodePath playerPath;
    [Export] private float timeMultiplier = 0.1f;
    [Export] private float minSpawnInterval = 0.5f;

    private readonly Vector2 _worldSize = new(2272, 1208);
    private Timer spawnTimer;
    private Timer rateIncreaseTimer;
    private Player player;
    private bool spawnMeleeNext = true;
    private float timeElapsed;

    public override void _Ready()
    {
        player = GetNode<Player>(playerPath);
        if (player == null) GD.PrintErr("Player node not found!");
        SetupTimers();
    }

    private void SetupTimers()
    {
        spawnTimer = new Timer
        {
            WaitTime = baseSpawnRate,
            Autostart = true,
            OneShot = false
        };
        spawnTimer.Timeout += SpawnEnemy;
        AddChild(spawnTimer);

        rateIncreaseTimer = new Timer
        {
            WaitTime = 1.0f,
            Autostart = true,
            OneShot = false
        };
        rateIncreaseTimer.Timeout += RateIncrease;
        AddChild(rateIncreaseTimer);
    }

    private void RateIncrease()
    {
        timeElapsed += 1.0f;
        float newInterval = baseSpawnRate / Mathf.Sqrt(1 + timeElapsed * timeMultiplier);
        newInterval = Mathf.Clamp(newInterval, minSpawnInterval, baseSpawnRate);
        spawnTimer.WaitTime = newInterval;
        GD.Print("Current spawn interval: ", newInterval.ToString("0.00"));
    }

    private void SpawnEnemy()
    {
        if (meleeEnemyScene == null || rangedEnemyScene == null || player == null)
        {
            GD.PrintErr("Missing required references!");
            return;
        }

        var selectedScene = spawnMeleeNext ? meleeEnemyScene : rangedEnemyScene;
        spawnMeleeNext = !spawnMeleeNext;

        Vector2 spawnPosition;
        bool validPosition = false;
        int attempts = 0;
        const int maxAttempts = 10;

        while (!validPosition && attempts < maxAttempts)
        {
            spawnPosition = GetRandomEdgePosition();
            if (spawnPosition.DistanceTo(player.GlobalPosition) >= 500)
            {
                validPosition = true;
                CreateEnemy(selectedScene, spawnPosition);
            }
            attempts++;
        }

        if (!validPosition) GD.Print("Failed to find valid spawn position");
    }

    private Vector2 GetRandomEdgePosition()
    {
        var edge = (int)(GD.Randi() % 4);
        float x = 0, y = 0;

        switch (edge)
        {
            case 0:
                x = (float)GD.RandRange(spawnMargin.X, _worldSize.X - spawnMargin.X);
                y = (float)GD.RandRange(0, spawnMargin.Y);
                break;
            case 1:
                x = (float)GD.RandRange(_worldSize.X - spawnMargin.X, _worldSize.X);
                y = (float)GD.RandRange(spawnMargin.Y, _worldSize.Y - spawnMargin.Y);
                break;
            case 2:
                x = (float)GD.RandRange(spawnMargin.X, _worldSize.X - spawnMargin.X);
                y = (float)GD.RandRange(_worldSize.Y - spawnMargin.Y, _worldSize.Y);
                break;
            case 3:
                x = (float)GD.RandRange(0, spawnMargin.X);
                y = (float)GD.RandRange(spawnMargin.Y, _worldSize.Y - spawnMargin.Y);
                break;
        }

        return new Vector2(x, y);
    }

    private void CreateEnemy(PackedScene scene, Vector2 position)
    {
        var enemy = scene.Instantiate<Node2D>();
        enemy.Position = position;
        GetParent().AddChild(enemy);
    }
}