using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	private bool active = false; // Start inactive
	private Timer lifeTimer;
	private Timer destructionTimer;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 1.0f;
	private const int ProjectileZIndex = 5;

	public override void _Ready()
	{
		if (!IsConnected(SignalName.BodyEntered, Callable.From<Node2D>(OnBodyEntered)))
		{
			BodyEntered += OnBodyEntered;
		}

		lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
		if (lifeTimer is null)
		{
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
		// No need to check ProcessMode here as it's implicitly handled by the engine
		// if (ProcessMode == ProcessModeEnum.Disabled || !active || Direction == Vector2.Zero)
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}
		GlobalPosition += Direction * Speed * (float)delta;
	}

	// Setup initial state WITHOUT activating
	public void Setup(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction;
		active = false; // Ensure inactive
		ZIndex = ProjectileZIndex;

		// Ensure starts disabled and invisible
		Visible = false;
		SetProcess(false);
		SetPhysicsProcess(false);
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);


		if (Sprite is not null)
		{
			// Sprite visibility controlled by root Visible property
			if (spriteTexture is not null) Sprite.Texture = spriteTexture;
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
			if (particleColor.HasValue) DestructionParticles.Color = particleColor.Value;
		}

		lifeTimer?.Stop();
		destructionTimer?.Stop();
	}

	// Activate the projectile (make visible, enable physics/processing, start timer)
	public void Activate()
	{
		if (active) return; // Prevent double activation

		active = true;
		Visible = true;
		SetProcess(true);
		SetPhysicsProcess(true);
		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
	}


	private void OnBodyEntered(Node2D body)
	{
		if (!active || body is not Player player) return;
		player.TakeDamage(1);
		player.ApplyKnockback(Direction * KnockbackForce);
		StartDestructionSequence();
	}

	private void OnLifeTimerTimeout()
	{
		if (!active) return;
		StartDestructionSequence();
	}

	private void StartDestructionSequence()
	{
		if (!active) return;
		active = false;
		lifeTimer?.Stop();
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		SetPhysicsProcess(false);
		// Keep process true briefly for destruction particle visibility? No, let timer handle return.
		// SetProcess(false);
		Visible = false; // Hide immediately
		if (Sprite != null) Sprite.Visible = false; // Should be redundant if Visible = false works
		Direction = Vector2.Zero;
		DestructionParticles?.Restart(); // Should still work even if parent is hidden? Test needed.
		destructionTimer?.Stop();
		destructionTimer?.Start(DestructionDuration);
	}

	private void ReturnToPool()
	{
		if (GlobalAudioPlayer.Instance is not null && SourceScene is not null)
		{
			GlobalAudioPlayer.Instance.ReturnProjectileToPool(this, SourceScene);
		}
		else
		{
			GD.PrintErr($"Projectile: Cannot return to pool. Freeing.");
			QueueFree();
		}
	}

	public void ResetForPooling()
	{
		active = false;
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		Visible = false;
		SetProcess(false);
		SetPhysicsProcess(false);
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		if (Sprite is not null) Sprite.Visible = false;
		if (DestructionParticles is not null) DestructionParticles.Emitting = false;
		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero; // Reset position
	}

	public override void _ExitTree()
	{
		if (IsConnected(SignalName.BodyEntered, Callable.From<Node2D>(OnBodyEntered)))
		{
			BodyEntered -= OnBodyEntered;
		}
		if (lifeTimer is not null && IsInstanceValid(lifeTimer) && lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
		{
			lifeTimer.Timeout -= OnLifeTimerTimeout;
		}
		if (destructionTimer is not null && IsInstanceValid(destructionTimer) && destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout -= ReturnToPool;
		}
		base._ExitTree();
	}
}
