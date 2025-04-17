using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	public int Health = 1;
	public int MaxHealth = 100;
	public Inventory Inventory = new();

	[Export] private Gun gun;
	[Export] private AudioStream damageAudio;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private CpuParticles2D deathParticles;
	[Export] private Node audioPlayerContainer;
	[Export] private Timer regenerationTimer;
	[Export] private NodePath cameraPath;
	[Export] private Timer deathPauseTimer;

	private ShakeyCamera camera;
	private Vector2 knockbackVelocity = Vector2.Zero;

	private const int RegenerationRate = 0;
	private const float Speed = 300.0f;
	private const float KnockbackRecoverySpeed = 0.1f;
	private const float DamageShakeMinStrength = 0.8f;
	private const float DamageShakeMaxStrength = 2.5f;
	private const float DamageShakeDuration = 0.3f;
	private const float DeathZoomAmount = 2.0f;
	private const float DeathZoomDuration = 1.5f;
	private const string SfxBusName = "SFX";

	public event Action GameOver;

	public override void _Ready()
	{
		if (cameraPath is not null)
		{
			camera = GetNode<ShakeyCamera>(cameraPath);
			if (IsInstanceValid(camera))
			{
				GD.Print("Player._Ready: Camera found and zoom reset.");
				camera.ResetZoom();
			}
			else
			{
				GD.PrintErr("Player._Ready: Camera node found at path, but instance is invalid!");
				camera = null;
			}
		}
		else
		{
			GD.PrintErr("Player: Camera Path not set!");
		}

		if (regenerationTimer is not null)
		{
			regenerationTimer.Timeout += OnRegenTimerTimeout;
		}
		else
		{
			GD.PrintErr("Player: RegenTimer not assigned!");
		}

		if (deathPauseTimer is not null)
		{
			deathPauseTimer.Timeout += OnDeathPauseTimerTimeout;
		}
		else
		{
			GD.PrintErr("Player: DeathPauseTimer not assigned in the scene tree or path!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused || (deathPauseTimer is not null && !deathPauseTimer.IsStopped()))
		{
			return;
		}

		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);

		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		Vector2 movement = direction * Speed + knockbackVelocity;
		Velocity = movement;
		MoveAndSlide();
	}

	public override void _ExitTree()
	{
		if (regenerationTimer is not null && regenerationTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)))
		{
			regenerationTimer.Timeout -= OnRegenTimerTimeout;
		}
		if (deathPauseTimer is not null && deathPauseTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathPauseTimerTimeout)))
		{
			deathPauseTimer.Timeout -= OnDeathPauseTimerTimeout;
		}

		base._ExitTree();
	}

	public void TakeDamage(int damage)
	{
		if (Health <= 0) return;

		Health -= damage;
		Health = Math.Max(Health, 0);

		if (damageParticles is not null)
		{
			damageParticles.Emitting = true;
		}

		PlayDamageSound();
		TriggerDamageShake();

		if (Health <= 0)
		{
			Die();
		}
	}

	private void TriggerDamageShake()
	{
		if (camera is null || !IsInstanceValid(camera))
		{
			return;
		}

		float healthRatio = MaxHealth > 0
			? Mathf.Clamp((float)Health / MaxHealth, 0f, 1f)
			: 0f;

		float shakeStrength = Mathf.Lerp(DamageShakeMaxStrength, DamageShakeMinStrength, healthRatio);

		camera.Shake(shakeStrength, DamageShakeDuration);
	}

	private void PlayDamageSound()
	{
		if (damageAudio is null || audioPlayerContainer is null)
		{
			return;
		}

		AudioStreamPlayer newAudioPlayer = new()
		{
			Stream = damageAudio,
			Bus = SfxBusName
		};

		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Finished += () => OnSingleAudioPlayerFinished(newAudioPlayer);
		newAudioPlayer.Play();
	}

	private void Die()
	{
		if ((deathPauseTimer is not null && !deathPauseTimer.IsStopped()) || GetTree().Paused)
		{
			GD.Print("Player.Die: Death sequence already in progress or game paused. Aborting.");
			return;
		}

		GD.Print("Player.Die: Player has died. Starting death sequence.");
		regenerationTimer?.Stop();
		ProcessMode = ProcessModeEnum.Disabled;
		SetPhysicsProcess(false);

		sprite.Visible = false;
		gun.Visible = false;
		deathParticles.Emitting = true;

		GD.Print($"Player.Die: Checking camera instance validity before zoom...");
		if (camera is not null && IsInstanceValid(camera))
		{
			GD.Print($"Player.Die: Camera instance is valid. Starting camera zoom to {DeathZoomAmount}x over {DeathZoomDuration}s.");
			camera.ZoomToPoint(DeathZoomAmount, DeathZoomDuration);
		}
		else
		{
			GD.PrintErr("Player.Die: Camera reference is null or invalid, cannot perform death zoom.");
		}

		GD.Print($"Player.Die: Starting DeathPauseTimer ({deathPauseTimer.WaitTime}s).");
		deathPauseTimer.Start();
	}

	private void OnDeathPauseTimerTimeout()
	{
		GD.Print("Player.OnDeathPauseTimerTimeout: Timer finished. Pausing tree and invoking GameOver.");

		if (!GetTree().Paused)
		{
			GetTree().Paused = true;
		}

		GameOver?.Invoke();
		GD.Print("Player.OnDeathPauseTimerTimeout: GameOver event invoked.");
	}

	public void ApplyKnockback(Vector2 knockback)
	{
		knockbackVelocity = knockbackVelocity.LengthSquared() < knockback.LengthSquared()
			? knockback
			: knockbackVelocity + knockback;
	}

	private void OnRegenTimerTimeout()
	{
		if (Health >= MaxHealth)
		{
			return;
		}

		Health = Math.Min(Health + RegenerationRate, MaxHealth);
	}

	private static void OnSingleAudioPlayerFinished(AudioStreamPlayer player)
	{
		if (player is null || !IsInstanceValid(player))
		{
			return;
		}
		player.QueueFree();
	}
}
