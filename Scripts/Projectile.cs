using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 1.0f; // Matches particle lifetime ideally
	public const int ProjectileZIndex = 5;


	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
		if (lifeTimer is null)
		{
			lifeTimer = new Timer { Name = "LifeTimer", OneShot = true };
			AddChild(lifeTimer);
		}
		lifeTimer.Timeout += OnLifeTimerTimeout;

		destructionTimer = GetNodeOrNull<Timer>("DestructionTimer");
		if (destructionTimer is null)
		{
			destructionTimer = new Timer { Name = "DestructionTimer", OneShot = true };
			AddChild(destructionTimer);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}
		GlobalPosition += Direction * Speed * (float)delta;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;

		if (lifeTimer is not null && IsInstanceValid(lifeTimer))
		{
			if (lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
			{
				lifeTimer.Timeout -= OnLifeTimerTimeout;
			}
		}
		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			if (destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
			{
				destructionTimer.Timeout -= ReturnToPool;
			}
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
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);


		if (Sprite is not null)
		{
			if (spriteTexture is not null)
			{
				Sprite.Texture = spriteTexture;
			}
			Sprite.Visible = true;
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
		if (destructionTimer is not null && destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout -= ReturnToPool;
		}
	}

	public void Activate()
	{
		if (active)
		{
			return;
		}

		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;

		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
	}


	private void OnBodyEntered(Node2D body)
	{
		if (!active)
		{
			return;
		}

		if (body is Player player)
		{
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			StartDestructionSequence();
		}
	}

	private void OnLifeTimerTimeout()
	{
		if (!active)
		{
			return;
		}
		StartDestructionSequence();
	}

	private void StartDestructionSequence()
	{
		if (!active)
		{
			return;
		}

		active = false;
		lifeTimer?.Stop();

		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled); // Use CallDeferred
		CallDeferred(CanvasItem.MethodName.SetVisible, false); // Defer visibility change too for safety

		Direction = Vector2.Zero;

		if (destructionTimer is not null)
		{
			if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
			{
				destructionTimer.Timeout += ReturnToPool;
			}
			destructionTimer.Start(DestructionDuration);
		}
		else
		{
			GetTree().CreateTimer(DestructionDuration, false, true).Timeout += ReturnToPool;
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.GlobalPosition = this.GlobalPosition;
			DestructionParticles.Restart();
		}
	}

	private void ReturnToPool()
	{
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}

		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			if (destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
			{
				destructionTimer.Timeout -= ReturnToPool;
			}
			destructionTimer.Stop();
		}


		if (GlobalAudioPlayer.Instance is not null && SourceScene is not null)
		{
			GlobalAudioPlayer.Instance.ReturnProjectileToPool(this, SourceScene);
		}
		else
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: Cannot return to pool. Freeing. GAP: {GlobalAudioPlayer.Instance}, Scene: {SourceScene}");
			QueueFree();
		}
	}

	public void ResetForPooling()
	{
		active = false;
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		if (destructionTimer is not null && destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout -= ReturnToPool;
		}

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;

		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);

		if (Sprite is not null)
		{
			Sprite.Visible = true;
		}
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}
		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero;
	}
}
