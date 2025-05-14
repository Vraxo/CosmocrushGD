using Godot;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	private const float Speed = 300.0f;
	private const float KnockbackRecoverySpeed = 0.1f;
	private const float DamageShakeMinStrength = 0.8f;
	private const float DamageShakeMaxStrength = 2.5f;
	private const float DamageShakeDuration = 0.3f;
	private const float DesktopDeathZoomAmount = 2.0f;
	private const float MobileDeathZoomAmount = 3.0f;
	private const float DeathZoomDuration = 1.5f;
	private const int RegenerationRate = 1;

	private ShakeyCamera camera;
	private Vector2 knockbackVelocity = Vector2.Zero;
	private AudioStream damageAudio;

	[Export] private Gun gun;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private CpuParticles2D deathParticles;
	[Export] private Timer regenerationTimer;
	[Export] private NodePath cameraPath;
	[Export] private Timer deathPauseTimer;
	[Export] private AudioStreamPlayer deathAudioPlayer;

	public int Health { get; set; } = 100;
	public int MaxHealth { get; set; } = 100;
	public Inventory Inventory { get; set; } = new();

	[Signal] public delegate void GameOverEventHandler();
	[Signal] public delegate void PlayerDiedEventHandler();

	public override void _Ready()
	{
		if (cameraPath is not null)
		{
			camera = GetNode<ShakeyCamera>(cameraPath);
			camera?.ResetZoom();
		}

		regenerationTimer.Timeout += OnRegenTimerTimeout;
		deathPauseTimer.Timeout += OnDeathPauseTimerTimeout;

		damageAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/PlayerDamage.mp3");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused || deathPauseTimer?.IsStopped() is false)
		{
			return;
		}

		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);

		var direction = Input.GetVector("left", "right", "up", "down");
		var movement = direction * Speed + knockbackVelocity;

		Velocity = movement;

		MoveAndSlide();
	}

	public override void _ExitTree()
	{
		regenerationTimer.Timeout -= OnRegenTimerTimeout;
		deathPauseTimer.Timeout -= OnDeathPauseTimerTimeout;

		base._ExitTree();
	}

	public void TakeDamage(int damage)
	{
		if (Health <= 0)
		{
			return;
		}

		Health -= damage;
		Health = Mathf.Max(Health, 0);

		if (damageParticles is not null)
		{
			damageParticles.GlobalPosition = GlobalPosition;
			damageParticles.Restart();
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
		knockbackVelocity = knockbackVelocity.LengthSquared() < knockback.LengthSquared()
			? knockback
			: knockbackVelocity + knockback;
	}

	private void TriggerDamageShake()
	{
		if (camera is null)
		{
			return;
		}

		var healthRatio = MaxHealth > 0
			? float.Clamp((float)Health / MaxHealth, 0f, 1f)
			: 0f;

		var shakeStrength = Mathf.Lerp( // Corrected: float.Lerp to Mathf.Lerp
			DamageShakeMaxStrength,
			DamageShakeMinStrength,
			healthRatio);

		camera.Shake(shakeStrength, DamageShakeDuration);
	}

	private void PlayDamageSound()
	{
		GlobalAudioPlayer.Instance?.PlaySound(damageAudio);
	}

	private void Die()
	{
		EmitSignal(SignalName.PlayerDied);
		regenerationTimer?.Stop();
		ProcessMode = ProcessModeEnum.Disabled;
		SetPhysicsProcess(false);

		sprite?.QueueFree();
		gun?.QueueFree();

		if (deathParticles is not null)
		{
			deathParticles.GlobalPosition = GlobalPosition;
			deathParticles.Restart();
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

	private void OnDeathPauseTimerTimeout()
	{
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

		Health = int.Min(Health + RegenerationRate, MaxHealth);
	}
}
