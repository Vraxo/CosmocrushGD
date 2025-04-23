// <file path="Projectile.cs">
using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;
	// SourceScene might still be useful if you mix pooling and non-pooling later,
	// but for now, it's not used for returning.
	public PackedScene SourceScene { get; set; }

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 1.0f; // How long particles live
	private const int ProjectileZIndex = 5;

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
		// Connect the timer timeout to the cleanup method
		destructionTimer.Timeout += CleanupAndFree; // Renamed for clarity
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}
		GlobalPosition += Direction * Speed * (float)delta;
	}

	public void Setup(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction;
		active = false;
		ZIndex = ProjectileZIndex;
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);


		if (Sprite is not null)
		{
			if (spriteTexture is not null) Sprite.Texture = spriteTexture;
			Sprite.Visible = true; // Sprite itself is visible within invisible parent initially
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
			if (particleColor.HasValue) DestructionParticles.Color = particleColor.Value;
		}

		lifeTimer?.Stop();
		destructionTimer?.Stop();
	}

	public void Activate()
	{
		if (active) return;

		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Inherit;
		SetPhysicsProcess(true);
		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
	}


	private void OnBodyEntered(Node2D body)
	{
		if (!active) return;

		if (body is Player player)
		{
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			StartDestructionSequence();
		}
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
		Visible = false; // Hide sprite/visuals

		// Make particles visible and emit them at current location
		if (DestructionParticles is not null)
		{
			// Reparent particles to the world or keep them global?
			// If TopLevel is true, setting GlobalPosition is enough.
			// Assuming DestructionParticles is configured as TopLevel=true in the scene.
			if (DestructionParticles.TopLevel)
			{
				DestructionParticles.GlobalPosition = this.GlobalPosition;
				DestructionParticles.Visible = true; // Ensure particles node itself is visible
				DestructionParticles.Restart(); // Start emitting
			}
			else
			{
				// If not TopLevel, they might get freed with the projectile.
				// Consider reparenting or making them TopLevel.
				GD.PushWarning("Projectile.DestructionParticles is not TopLevel, might disappear prematurely.");
				DestructionParticles.Emitting = true;
			}
		}

		// Stop any residual movement calculation
		Direction = Vector2.Zero;

		// Start timer to wait for particles before freeing the projectile node
		destructionTimer?.Stop();
		destructionTimer?.Start(DestructionDuration);
	}

	// Renamed from ReturnToPool for clarity, as it now just frees the instance.
	private void CleanupAndFree()
	{
		// Stop particles just before freeing, although they might continue if TopLevel
		if (DestructionParticles is not null && !DestructionParticles.TopLevel)
		{
			DestructionParticles.Emitting = false;
		}

		// --- Directly free the node instead of returning to pool ---
		QueueFree();
	}

	// ResetForPooling is likely not needed if these projectiles are never pooled,
	// but we leave it here in case pooling is used elsewhere or reintroduced.
	public void ResetForPooling()
	{
		active = false;
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
		SetPhysicsProcess(false);
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);

		if (Sprite is not null) Sprite.Visible = true;
		if (DestructionParticles is not null) DestructionParticles.Emitting = false;
		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;

		if (lifeTimer is not null && IsInstanceValid(lifeTimer))
		{
			lifeTimer.Timeout -= OnLifeTimerTimeout;
		}
		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			// Ensure signal is disconnected using the correct method name
			destructionTimer.Timeout -= CleanupAndFree;
		}
		base._ExitTree();
	}
}
// </file>
