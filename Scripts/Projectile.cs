// MODIFIED: Projectile.cs
// Summary: Explicitly set a positive ZIndex in the Setup method to ensure projectiles render above the background.
// - Added `ZIndex = 5;` within the Setup method. (Value 5 is arbitrary, just needs to be > 0).
using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	private bool active = true;
	private Timer lifeTimer;
	private Timer destructionTimer;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 1.0f;
	private const int ProjectileZIndex = 5; // Ensure projectiles draw above default background

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
		if (ProcessMode == ProcessModeEnum.Disabled || !active || Direction == Vector2.Zero)
		{
			return;
		}
		GlobalPosition += Direction * Speed * (float)delta;
	}

	public void Setup(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction;
		active = true;
		ZIndex = ProjectileZIndex; // Ensure rendering order

		SetProcess(true);
		SetPhysicsProcess(true);
		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		if (Sprite is not null)
		{
			Sprite.Visible = true;
			if (spriteTexture is not null) Sprite.Texture = spriteTexture;
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
			if (particleColor.HasValue) DestructionParticles.Color = particleColor.Value;
		}

		lifeTimer?.Stop();
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
		if (Sprite != null) Sprite.Visible = false;
		Direction = Vector2.Zero;
		DestructionParticles?.Restart();
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
		SetProcess(false);
		SetPhysicsProcess(false);
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		if (Sprite is not null) Sprite.Visible = false;
		if (DestructionParticles is not null) DestructionParticles.Emitting = false;
		Direction = Vector2.Zero;
		// Reset ZIndex? Or assume Setup will always set it? Let Setup handle it.
		// ZIndex = 0;
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
