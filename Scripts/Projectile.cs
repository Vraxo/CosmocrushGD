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
	private const float DestructionDuration = 1.0f;
	private const int ProjectileZIndex = 5;

	public override void _Ready()
	{
		// Use += syntax for signal connection
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
		destructionTimer.Timeout += ReturnToPool;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Early exit if not active or no direction
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
		active = false; // Ensure inactive initially
		ZIndex = ProjectileZIndex;

		// Ensure starts disabled and invisible
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled; // Explicitly disable processing
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);


		if (Sprite is not null)
		{
			// Sprite visibility controlled by root Visible property
			if (spriteTexture is not null) Sprite.Texture = spriteTexture;
			Sprite.Visible = true; // Ensure sprite itself is marked visible within the invisible parent
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
		if (active) return; // Prevent double activation

		active = true;
		Visible = true; // Make the whole node visible

		// Explicitly enable physics processing and potentially regular process if needed
		ProcessMode = ProcessModeEnum.Inherit; // Allow _Process and _PhysicsProcess if parent does
		SetPhysicsProcess(true); // **Explicitly enable physics**
								 // SetProcess(true); // Enable if _Process logic exists

		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
	}


	private void OnBodyEntered(Node2D body)
	{
		if (!active) return; // Check if active before processing collision

		if (body is Player player)
		{
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			StartDestructionSequence();
		}
		// Optional: Check for collisions with other things like walls if needed
		// else if (body is StaticBody2D || body is TileMap)
		// {
		//     StartDestructionSequence();
		// }
	}

	private void OnLifeTimerTimeout()
	{
		if (!active) return; // Check if active before timing out
		StartDestructionSequence();
	}

	private void StartDestructionSequence()
	{
		if (!active) return; // Prevent double destruction calls

		active = false;
		lifeTimer?.Stop();
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		SetPhysicsProcess(false); // Disable physics processing immediately
		Visible = false; // Hide visually

		if (Sprite is not null)
		{
			Sprite.Visible = false; // Should be redundant now
		}

		Direction = Vector2.Zero; // Stop any potential residual movement calculation attempt

		if (DestructionParticles is not null)
		{
			DestructionParticles.GlobalPosition = this.GlobalPosition; // Ensure particles emit at the right spot
			DestructionParticles.Restart(); // Restart particle emission
		}

		destructionTimer?.Stop(); // Ensure no previous timer is running
		destructionTimer?.Start(DestructionDuration);
	}

	private void ReturnToPool()
	{
		// Stop particles just before returning, in case timer was shorter than lifetime
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}

		if (GlobalAudioPlayer.Instance is not null && SourceScene is not null)
		{
			GlobalAudioPlayer.Instance.ReturnProjectileToPool(this, SourceScene);
		}
		else
		{
			GD.PrintErr($"Projectile: Cannot return to pool. Freeing. GAP: {GlobalAudioPlayer.Instance}, Scene: {SourceScene}");
			QueueFree();
		}
	}

	public void ResetForPooling()
	{
		active = false;
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled; // Ensure processing is fully disabled
		SetPhysicsProcess(false); // Explicitly disable physics
								  // SetProcess(false); // Explicitly disable process if it was ever enabled

		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);

		if (Sprite is not null)
		{
			Sprite.Visible = true; // Reset sprite visibility for next use
		}
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false; // Stop emitting
		}
		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero; // Reset position
	}

	public override void _ExitTree()
	{
		// Use -= syntax for signal disconnection
		BodyEntered -= OnBodyEntered;

		if (lifeTimer is not null && IsInstanceValid(lifeTimer))
		{
			lifeTimer.Timeout -= OnLifeTimerTimeout;
		}
		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			destructionTimer.Timeout -= ReturnToPool;
		}
		base._ExitTree();
	}
}
