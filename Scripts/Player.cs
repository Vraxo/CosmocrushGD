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

	private ShakeyCamera camera;
	private Vector2 knockbackVelocity = Vector2.Zero;

	private const int RegenerationRate = 0;
	private const float Speed = 300.0f;
	private const float KnockbackRecoverySpeed = 0.1f;
	private const float DamageShakeMinStrength = 0.8f;
	private const float DamageShakeMaxStrength = 2.5f;
	private const float DamageShakeDuration = 0.3f;
	private const string SfxBusName = "SFX";

	public event Action AudioPlayerFinished;
	public event Action GameOver;

	public override void _Ready()
	{
		gun = GetNode<Gun>("Gun");
		sprite = GetNode<Sprite2D>("Sprite");

		if (cameraPath is not null)
		{
			camera = GetNode<ShakeyCamera>(cameraPath);
		}
		else
		{
			GD.PrintErr("Player: Camera Path not set!");
		}

		AudioPlayerFinished += OnAudioPlayerFinished;

		if (regenerationTimer is not null)
		{
			regenerationTimer.Timeout += OnRegenTimerTimeout;
		}
		else
		{
			GD.PrintErr("Player: RegenTimer not assigned!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused)
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

		base._ExitTree();
	}

	public void TakeDamage(int damage)
	{
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
		if (camera is null)
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

	private void OnAudioPlayerFinished()
	{
		foreach (Node child in audioPlayerContainer.GetChildren())
		{
			if (child is AudioStreamPlayer audioPlayer && !audioPlayer.Playing)
			{
				audioPlayer.QueueFree();
			}
			else if (child is AudioStreamPlayer2D audioPlayer2D && !audioPlayer2D.Playing)
			{
				audioPlayer2D.QueueFree();
			}
		}
	}

	private void Die()
	{
		if (GetTree().Paused)
		{
			GD.Print("Player.Die: Already paused, aborting Die logic.");
			return;
		}

		GD.Print("Player.Die: Player has died.");
		regenerationTimer?.Stop();
		ProcessMode = ProcessModeEnum.Disabled;
		SetPhysicsProcess(false);

		if (sprite is not null)
		{
			sprite.Visible = false;
		}
		if (deathParticles is not null)
		{
			deathParticles.Emitting = true;
		}


		GD.Print("Player.Die: Pausing tree...");
		GetTree().Paused = true;

		GD.Print("Player.Die: Invoking GameOver event...");
		GameOver?.Invoke();
		GD.Print("Player.Die: GameOver event invoked (if any listeners).");
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
