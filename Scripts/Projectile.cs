using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public const int ProjectileZIndex = 5;

	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer;
	private Color? _currentParticleColor;

	[Export] public Sprite2D Sprite { get; private set; }
	[Export] public CpuParticles2D destructionParticles { get; private set; }

	public Vector2 Direction { get; private set; } = Vector2.Zero;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		ZIndex = ProjectileZIndex;

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
		if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(SelfDestruct)))
		{
			destructionTimer.Timeout += SelfDestruct;
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

		if (destructionTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(SelfDestruct)) ?? false)
		{
			destructionTimer.Timeout -= SelfDestruct;
		}

		base._ExitTree();
	}

	public void SetupAndActivate(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		if (active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already active, ignoring SetupAndActivate call.");
			return;
		}

		GlobalPosition = startPosition;
		Direction = direction.Normalized();
		_currentParticleColor = particleColor;

		if (Sprite is not null)
		{
			if (spriteTexture is not null)
			{
				Sprite.Texture = spriteTexture;
			}
			if (_currentParticleColor.HasValue)
			{
				Sprite.Modulate = _currentParticleColor.Value;
			}
			Sprite.Visible = true;
		}

		lifeTimer?.Stop();
		destructionTimer?.Stop();

		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;
		SetDeferred(PropertyName.Monitoring, true);
		SetDeferred(PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
		GD.Print($"Projectile {GetInstanceId()}: Setup and Activated.");
	}

	private void StartDestructionSequence()
	{
		if (!active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already destroying or inactive, ignoring StartDestructionSequence call.");
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Starting destruction sequence.");
		active = false;
		lifeTimer?.Stop();

		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
		CallDeferred(CanvasItem.MethodName.SetVisible, false);
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		Direction = Vector2.Zero;

		if (destructionParticles is not null)
		{
			destructionParticles.GlobalPosition = GlobalPosition;
			if (_currentParticleColor.HasValue)
			{
				destructionParticles.Modulate = _currentParticleColor.Value;
			}
			else if (Sprite?.Modulate is Color spriteModulate)
			{
				destructionParticles.Modulate = spriteModulate;
			}
			destructionParticles.Restart();
			destructionParticles.Emitting = true;
			GD.Print($"Projectile {GetInstanceId()}: Started destruction particles at {destructionParticles.GlobalPosition}.");
		}
		else
		{
			GD.PrintErr($"Projectile ({GetInstanceId()}): destructionParticles node not assigned.");
		}

		if (destructionTimer is not null)
		{
			destructionTimer.Start(DestructionDuration);
			GD.Print($"Projectile {GetInstanceId()}: Started destruction timer ({DestructionDuration}s).");
		}
		else
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: DestructionTimer node missing! Using temporary timer.");
			GetTree().CreateTimer(DestructionDuration, false, true).Timeout += SelfDestruct;
		}
	}

	private void SelfDestruct()
	{
		GD.Print($"Projectile {GetInstanceId()}: SelfDestruct called.");
		destructionTimer?.Stop();
		QueueFree();
		GD.Print($"Projectile {GetInstanceId()}: Freed.");
	}

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
		else if (body is BaseEnemy)
		{
		}
		else if (body.IsInGroup("Obstacles") || body is StaticBody2D)
		{
			GD.Print($"Projectile {GetInstanceId()}: Hit Obstacle/StaticBody.");
			StartDestructionSequence();
		}
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

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 0.6f;
}
