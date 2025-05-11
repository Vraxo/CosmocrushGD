using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : Area2D
{
	// --- Signals ---
	[Signal] public delegate void GameOverEventHandler();
	[Signal] public delegate void PlayerDiedEventHandler();

	// --- Fields ---
	private ShakeyCamera camera;
	private Vector2 currentVelocity = Vector2.Zero; // Use a custom velocity for Area2D
	private Vector2 knockbackVelocity = Vector2.Zero;
	private AudioStream damageAudio;
	private const float Speed = 300.0f;
	private const float KnockbackRecoverySpeed = 0.1f;
	private const float DamageShakeMinStrength = 0.8f;
	private const float DamageShakeMaxStrength = 2.5f;
	private const float DamageShakeDuration = 0.3f;
	private const float DesktopDeathZoomAmount = 2.0f;
	private const float MobileDeathZoomAmount = 3.0f;
	private const float DeathZoomDuration = 1.5f;
	private const int RegenerationRate = 0; // Keep const separate
	private const float PushForce = 100.0f; // Force to push overlapping areas

	// --- Properties ---
	public int Health { get; set; } = 30000; // Initialize with MaxHealth potential
	public int MaxHealth { get; set; } = 30000;
	public Inventory Inventory { get; set; } = new();

	// --- Exports ---
	[Export] private Gun gun;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private CpuParticles2D deathParticles;
	[Export] private Timer regenerationTimer;
	[Export] private NodePath cameraPath;
	[Export] private Timer deathPauseTimer;
	[Export] private AudioStreamPlayer deathAudioPlayer;


	// --- Methods ---
	public override void _Ready()
	{
		if (cameraPath is not null)
		{
			camera = GetNode<ShakeyCamera>(cameraPath);
			camera?.ResetZoom(); // Use null propagation
		}

		if (regenerationTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)) is false)
		{
			regenerationTimer.Timeout += OnRegenTimerTimeout;
		}

		if (deathPauseTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathPauseTimerTimeout)) is false)
		{
			deathPauseTimer.Timeout += OnDeathPauseTimerTimeout;
		}

		// Connect AreaEntered signal for soft collisions
		AreaEntered += OnAreaEntered;

		damageAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/PlayerDamage.mp3");
	}

	public override void _Process(double delta)
	{
		// Early return if paused or death timer active
		if (GetTree().Paused || deathPauseTimer?.IsStopped() is false)
		{
			return;
		}

		float fDelta = (float)delta;

		// Handle movement manually
		var direction = Input.GetVector("left", "right", "up", "down");
		currentVelocity = direction * Speed;

		// Apply knockback
		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);
		currentVelocity += knockbackVelocity;

		GlobalPosition += currentVelocity * fDelta;
	}

	public override void _ExitTree()
	{
		if (regenerationTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)) ?? false)
		{
			regenerationTimer.Timeout -= OnRegenTimerTimeout;
		}

		if (deathPauseTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathPauseTimerTimeout)) ?? false)
		{
			deathPauseTimer.Timeout -= OnDeathPauseTimerTimeout;
		}

		// Disconnect AreaEntered signal
		AreaEntered -= OnAreaEntered;

		base._ExitTree();
	}

	// Method to apply push force from soft collisions
	public void ApplyPush(Vector2 force)
	{
		currentVelocity += force;
	}

	public void TakeDamage(int damage)
	{
		if (Health <= 0)
		{
			return;
		}

		Health -= damage;
		Health = Mathf.Max(Health, 0); // Use Mathf.Max

		// Restart damage particles at current location (now TopLevel)
		if (damageParticles is not null)
		{
			damageParticles.GlobalPosition = GlobalPosition;
			damageParticles.Restart(); // Use Restart() for one-shot TopLevel particles
		}

		PlayDamageSound();
		TriggerDamageShake();

		if (Health <= 0)
		{
			Die();
		}
	}

	public void ApplyKnockback(Vector2 knockback)
	{
		// Apply stronger knockback if the new one is larger, otherwise add
		knockbackVelocity = knockbackVelocity.LengthSquared() < knockback.LengthSquared()
			? knockback
			: knockbackVelocity + knockback;
	}

	// --- Private Methods ---
	private void TriggerDamageShake()
	{
		if (camera is null) // No need for IsInstanceValid check here if camera is assigned in _Ready
		{
			return;
		}

		var healthRatio = MaxHealth > 0
			? float.Clamp((float)Health / MaxHealth, 0f, 1f) // Use float.Clamp
			: 0f;

		var shakeStrength = Mathf.Lerp(DamageShakeMaxStrength, DamageShakeMinStrength, healthRatio);

		camera.Shake(shakeStrength, DamageShakeDuration);
	}

	private void PlayDamageSound()
	{
		GlobalAudioPlayer.Instance?.PlaySound(damageAudio); // Use null propagation
	}

	private void Die()
	{
		EmitSignal(SignalName.PlayerDied);
		regenerationTimer?.Stop();
		ProcessMode = ProcessModeEnum.Disabled;
		// SetPhysicsProcess(false); // Not needed for Area2D

		sprite?.QueueFree(); // Remove sprite immediately
		gun?.QueueFree();   // Remove gun immediately

		// Restart death particles at current location (now TopLevel)
		if (deathParticles is not null)
		{
			deathParticles.GlobalPosition = GlobalPosition;
			deathParticles.Restart(); // Use Restart() for one-shot TopLevel particles
		}

		deathAudioPlayer?.Play();

		if (camera is not null)
		{
			var zoomAmount = OS.HasFeature("mobile")
				? MobileDeathZoomAmount
				: DesktopDeathZoomAmount;
			camera.ZoomToPoint(zoomAmount, DeathZoomDuration);
		}

		deathPauseTimer?.Start();
	}

	// --- Event Handlers ---
	private void OnDeathPauseTimerTimeout()
	{
		// Only pause if not already paused (safety check)
		if (!GetTree().Paused)
		{
			GetTree().Paused = true;
		}

		EmitSignal(SignalName.GameOver);
	}

	private void OnRegenTimerTimeout()
	{
		if (Health >= MaxHealth)
		{
			return;
		}

		Health = Math.Min(Health + RegenerationRate, MaxHealth);
	}

	private void OnAreaEntered(Area2D area)
	{
		// Basic soft collision: push overlapping areas away
		if (area is BaseEnemy enemy)
		{
			Vector2 pushDirection = (GlobalPosition - enemy.GlobalPosition).Normalized();
			// Apply force to both the player and the enemy
			currentVelocity += pushDirection * PushForce * (float)GetProcessDeltaTime();
			enemy.ApplyPush(pushDirection * PushForce * (float)GetProcessDeltaTime()); // Assuming BaseEnemy has an ApplyPush method
		}
	}
}
