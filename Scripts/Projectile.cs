using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;
	[Export] private Timer lifeTimer;
	[Export] private Timer destructionTimer; // Remains for particle effect duration on collision

	public Vector2 Direction { get; private set; } = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	public const int ProjectileZIndex = 5;

	private bool active = false;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 0.6f; // For particle effect

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		lifeTimer.Timeout += OnLifeTimerTimeout;
		destructionTimer.Timeout += OnDestructionTimerFinished;
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
		if (IsInstanceValid(this)) // Check if the instance itself is valid before proceeding
		{
			if (lifeTimer is not null && IsInstanceValid(lifeTimer))
			{
				lifeTimer.Timeout -= OnLifeTimerTimeout;
			}
			if (destructionTimer is not null && IsInstanceValid(destructionTimer))
			{
				destructionTimer.Timeout -= OnDestructionTimerFinished;
			}
			// BodyEntered -= OnBodyEntered; // This can cause issues if already freed or being freed.
			// Generally, Godot handles signal disconnections on queue_free/free.
			// If explicit disconnection is needed, ensure it's safe.
			// For signals connected in _Ready from the same node, it's usually fine to let Godot handle it.
		}
		base._ExitTree();
	}

	public void SetupAndActivate(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction.Normalized();

		if (spriteTexture is not null)
		{
			Sprite.Texture = spriteTexture;
		}
		// If spriteTexture is null, it will use the default from Projectile.tscn

		Sprite.Visible = true;

		DestructionParticles.Emitting = false; // Ensure particles are reset
		if (particleColor.HasValue)
		{
			DestructionParticles.Color = particleColor.Value;
		}
		// If particleColor is null, it will use the default from Projectile.tscn or last set
		DestructionParticles.Position = Vector2.Zero;

		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;

		SetDeferred(PropertyName.Monitoring, true);
		SetDeferred(PropertyName.Monitorable, true);

		lifeTimer.Start(DefaultLifetime);
		destructionTimer.Stop();
	}

	public void ResetForPooling()
	{
		active = false;

		lifeTimer.Stop();
		destructionTimer.Stop();

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		DestructionParticles.Emitting = false;
		Sprite.Visible = false;
		// Optionally reset Sprite.Texture to a default if necessary,
		// but SetupAndActivate should handle setting it correctly each time.

		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero;
	}

	private void StartDestructionSequenceWithParticles()
	{
		active = false;
		lifeTimer.Stop();

		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
		CallDeferred(CanvasItem.MethodName.SetVisible, false);
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		Direction = Vector2.Zero;

		DestructionParticles.GlobalPosition = this.GlobalPosition;
		DestructionParticles.Restart();
		destructionTimer.Start(DestructionDuration);
	}

	private void ReturnToPool()
	{
		ResetForPooling();
		ProjectilePoolManager.Instance.ReturnProjectileToPool(this);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!active)
		{
			return;
		}

		bool hitValidTarget = false;
		if (body is Player player)
		{
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			hitValidTarget = true;
		}
		else if (body is BaseEnemy /* enemy */)
		{
			// Currently, projectiles don't damage other enemies by default.
			// If they should:
			// enemy.TakeDamage(1); // Example damage
			// hitValidTarget = true;
		}
		else if (body.IsInGroup("Obstacles") || body is StaticBody2D)
		{
			hitValidTarget = true;
		}

		if (hitValidTarget)
		{
			StartDestructionSequenceWithParticles(); // Play particles and use destructionTimer
		}
	}

	private void OnLifeTimerTimeout()
	{
		if (!active)
		{
			return;
		}
		active = false;
		ReturnToPool(); // Projectile simply disappears, no particle effect or destructionTimer involved
	}

	private void OnDestructionTimerFinished()
	{
		// This is only called if StartDestructionSequenceWithParticles was invoked.
		// 'active' should already be false.
		ReturnToPool();
	}
}
