using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CosmocrushGD;

public partial class EnemyPoolManager : Node
{
	[Signal]
	public delegate void PoolInitializationCompleteEventHandler();

	[Export] private PackedScene meleeEnemyScene;
	[Export] private PackedScene rangedEnemyScene;
	[Export] private PackedScene explodingEnemyScene;
	[Export] private PackedScene tankEnemyScene;
	[Export] private PackedScene swiftEnemyScene;
	[Export] private int initialPoolSizeMelee = 20;
	[Export] private int initialPoolSizeRanged = 15;
	[Export] private int initialPoolSizeExploding = 10;
	[Export] private int initialPoolSizeTank = 5;
	[Export] private int initialPoolSizeSwift = 15;
	[Export] private NodePath enemyContainerPath;
	[Export] private float initializationDelay = 0.5f;
	[Export] private int enemiesPerFrame = 1;

	private Dictionary<PackedScene, Queue<BaseEnemy>> availableEnemies = new();
	private Dictionary<PackedScene, int> targetPoolCounts = new();
	private List<PackedScene> scenesToInitialize = new();
	private Node enemyContainer;
	private bool initializationStarted = false;
	private bool initializationComplete = false;
	private Timer delayTimer;

	public override void _Ready()
	{
		enemyContainer = GetNode<Node>(enemyContainerPath);
		if (enemyContainer is null)
		{
			enemyContainer = this;
		}
		else
		{
		}

		SetupPool(meleeEnemyScene, initialPoolSizeMelee);
		SetupPool(rangedEnemyScene, initialPoolSizeRanged);
		SetupPool(explodingEnemyScene, initialPoolSizeExploding);
		SetupPool(tankEnemyScene, initialPoolSizeTank);
		SetupPool(swiftEnemyScene, initialPoolSizeSwift);

		if (scenesToInitialize.Count > 0)
		{
			delayTimer = new Timer();
			delayTimer.WaitTime = initializationDelay;
			delayTimer.OneShot = true;
			delayTimer.Timeout += OnInitializationDelayTimeout;
			AddChild(delayTimer);
			delayTimer.Start();
		}
		else
		{
			initializationComplete = true;
			EmitSignal(SignalName.PoolInitializationComplete);
		}

		SetProcess(false);
	}

	private void OnInitializationDelayTimeout()
	{
		initializationStarted = true;
		SetProcess(true);
		delayTimer?.QueueFree();
		delayTimer = null;
	}


	private void SetupPool(PackedScene scene, int count)
	{
		if (scene is null || count <= 0)
		{
			return;
		}

		if (!availableEnemies.ContainsKey(scene))
		{
			availableEnemies.Add(scene, new Queue<BaseEnemy>());
			targetPoolCounts.Add(scene, count);
			scenesToInitialize.Add(scene);
		}
		else
		{
		}
	}

	public override void _Process(double delta)
	{
		if (!initializationStarted || initializationComplete)
		{
			return;
		}

		int initializedThisFrame = 0;
		bool allPoolsFilled = true;

		foreach (var scene in scenesToInitialize.ToList())
		{
			if (!availableEnemies.TryGetValue(scene, out var queue) || !targetPoolCounts.TryGetValue(scene, out var targetCount))
			{
				continue;
			}

			while (queue.Count < targetCount && initializedThisFrame < enemiesPerFrame)
			{
				InstantiateAndPoolEnemy(scene, queue);
				initializedThisFrame++;
				if (queue.Count < targetCount)
				{
					allPoolsFilled = false;
				}
			}

			if (queue.Count < targetCount)
			{
				allPoolsFilled = false;
			}

			if (initializedThisFrame >= enemiesPerFrame)
			{
				break;
			}
		}

		if (allPoolsFilled)
		{
			initializationComplete = true;
			EmitSignal(SignalName.PoolInitializationComplete);
			SetProcess(false);
		}
	}


	private void InstantiateAndPoolEnemy(PackedScene scene, Queue<BaseEnemy> queue)
	{
		BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
		enemy.PoolManager = this;
		enemy.SourceScene = scene;
		AddChild(enemy);
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.Visible = false;
		if (enemy.Collider is not null)
		{
			enemy.Collider.Disabled = true;
		}
		queue.Enqueue(enemy);
	}


	public BaseEnemy GetEnemy(PackedScene scene)
	{
		if (scene is null)
		{
			return null;
		}

		if (!availableEnemies.TryGetValue(scene, out Queue<BaseEnemy> queue))
		{
			return InstantiateNewEnemyFallback(scene);
		}

		if (queue.Count > 0)
		{
			BaseEnemy enemy = queue.Dequeue();

			if (enemy is null || !IsInstanceValid(enemy))
			{
				return InstantiateNewEnemyFallback(scene);
			}

			if (enemyContainer is not null && enemy.GetParent() != enemyContainer)
			{
				enemy.GetParent()?.RemoveChild(enemy);
				enemyContainer.AddChild(enemy);
			}

			return enemy;
		}
		else
		{
			if (!initializationComplete)
			{
			}
			else
			{
			}
			return InstantiateNewEnemyFallback(scene);
		}
	}


	private BaseEnemy InstantiateNewEnemyFallback(PackedScene scene)
	{
		if (scene is null)
		{
			return null;
		}

		BaseEnemy enemy = scene.Instantiate<BaseEnemy>();
		enemy.PoolManager = this;
		enemy.SourceScene = scene;

		if (enemyContainer is not null)
		{
			enemyContainer.AddChild(enemy);
		}
		else
		{
			AddChild(enemy);
		}

		return enemy;
	}

	public void ReturnEnemy(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			return;
		}

		if (enemy.SourceScene is null)
		{
			enemy.QueueFree();
			return;
		}

		if (!availableEnemies.TryGetValue(enemy.SourceScene, out Queue<BaseEnemy> queue))
		{
			enemy.QueueFree();
			return;
		}

		enemy.ProcessMode = ProcessModeEnum.Disabled;
		enemy.SetPhysicsProcess(false);
		enemy.SetProcess(false);
		enemy.Visible = false;
		enemy.TargetPlayer = null;

		if (enemy.Collider is not null)
		{
			enemy.Collider.Disabled = true;
		}

		if (enemy.DamageParticles is not null)
		{
			enemy.DamageParticles.Emitting = false;
		}
		if (enemy.DeathParticles is not null)
		{
			enemy.DeathParticles.Emitting = false;
		}


		if (enemy.GetParent() != this)
		{
			enemy.GetParent()?.RemoveChild(enemy);
			AddChild(enemy);
		}

		queue.Enqueue(enemy);
	}

	public override void _ExitTree()
	{
		if (delayTimer is not null && IsInstanceValid(delayTimer))
		{
			if (delayTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnInitializationDelayTimeout)))
			{
				delayTimer.Timeout -= OnInitializationDelayTimeout;
			}
		}
		base._ExitTree();
	}
}
