using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	// --- Properties ---
	public Vector2 Direction { get; private set; } = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	// --- Exports ---
	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	// --- Private Fields ---
	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer;

	// --- Constants ---
	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 0.6f;
	public const int ProjectileZIndex = 5;

	// --- Methods ---

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
		if (lifeTimer is null)
		{
			GD.Print("Projectile: LifeTimer not found, creating one.");
			lifeTimer = new Timer { Name = "LifeTimer", OneShot = true };
			AddChild(lifeTimer);
		}

		if (!lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
		{
			lifeTimer.Timeout += OnLifeTimerTimeout;
		}

		destructionTimer = GetNodeOrNull<Timer>("DestructionTimer");
		if (destructionTimer is null)
		{
			GD.Print("Projectile: DestructionTimer not found, creating one.");
			destructionTimer = new Timer { Name = "DestructionTimer", OneShot = true };
			AddChild(destructionTimer);
		}

		if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout += ReturnToPool;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}

		var movement = Direction * Speed * (float)delta;

		GlobalPosition += movement;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(this))
		{
			BodyEntered -= OnBodyEntered;
		}

		if (lifeTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)) ?? false)
		{
			lifeTimer.Timeout -= OnLifeTimerTimeout;
		}

		if (destructionTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)) ?? false)
		{
			destructionTimer.Timeout -= ReturnToPool;
		}

		base._ExitTree();
	}

	public void Setup(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction.Normalized();
		active = false;

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		if (Sprite is not null)
		{
			if (spriteTexture is not null)
			{
				Sprite.Texture = spriteTexture;
			}
			Sprite.Visible = true; // Controlled by parent visibility
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
			if (particleColor.HasValue)
			{
				DestructionParticles.Color = particleColor.Value;
			}
			DestructionParticles.Position = Vector2.Zero;
		}

		lifeTimer?.Stop();
		destructionTimer?.Stop();
	}

	public void Activate()
	{
		if (active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already active, ignoring Activate call.");
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Activating.");
		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;

		SetDeferred(PropertyName.Monitoring, true);
		SetDeferred(PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
	}

	public void ResetForPooling()
	{
		GD.Print($"Projectile {GetInstanceId()}: Resetting for pooling.");
		active = false;

		lifeTimer?.Stop();
		destructionTimer?.Stop();

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;

		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}

		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero;
	}

	private void StartDestructionSequence()
	{
		if (!active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already destroying, ignoring StartDestructionSequence call.");
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Starting destruction sequence.");
		active = false;
		lifeTimer?.Stop();

		// Correctly use SetDeferred for properties
		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
		CallDeferred(CanvasItem.MethodName.SetVisible, false);
		SetDeferred(PropertyName.Monitoring, false);     // Fixed: Use SetDeferred
		SetDeferred(PropertyName.Monitorable, false);    // Fixed: Use SetDeferred

		Direction = Vector2.Zero;

		if (DestructionParticles is not null)
		{
			DestructionParticles.GlobalPosition = this.GlobalPosition;
			DestructionParticles.Restart();
			GD.Print($"Projectile {GetInstanceId()}: Started destruction particles.");
		}

		if (destructionTimer is not null)
		{
			destructionTimer.Start(DestructionDuration);
			GD.Print($"Projectile {GetInstanceId()}: Started destruction timer ({DestructionDuration}s).");
		}
		else
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: DestructionTimer node missing! Using temporary timer.");
			GetTree().CreateTimer(DestructionDuration, false, true).Timeout += ReturnToPool;
		}
	}

	private void ReturnToPool()
	{
		GD.Print($"Projectile {GetInstanceId()}: ReturnToPool called.");

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}

		destructionTimer?.Stop();

		var poolManager = ProjectilePoolManager.Instance;

		if (poolManager is null)
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: Cannot return to pool. ProjectilePoolManager instance not found. Freeing.");
			QueueFree();
			return;
		}

		poolManager.ReturnProjectileToPool(this);
		GD.Print($"Projectile {GetInstanceId()}: Returned to pool via ProjectilePoolManager.");
	}

	// --- Event Handlers ---

	private void OnBodyEntered(Node2D body)
	{
		if (!active)
		{
			return;
		}

		if (body is Player player)
		{
			GD.Print($"Projectile {GetInstanceId()}: Hit Player.");
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			StartDestructionSequence();
		}
		// else if (body is Obstacle obstacle) // Example check
		// {
		//     GD.Print($"Projectile {GetInstanceId()}: Hit Obstacle.");
		//     StartDestructionSequence();
		// }
	}

	private void OnLifeTimerTimeout()
	{
		if (!active)
		{
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Life timer timeout.");
		StartDestructionSequence();
	}
}
