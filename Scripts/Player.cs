using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	public int Health = 100;
	public int MaxHealth = 100;
	public Inventory Inventory = new();

	public const float Speed = 300.0f;
	private const int RegenAmount = 1;

	[Export] private Gun gun;
	[Export] private AudioStream damageAudio;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private Node audioPlayerContainer;
	[Export] private Timer regenTimer;
	[Export] private NodePath cameraPath;

	private ShakeyCamera camera;
	private Vector2 knockbackVelocity = Vector2.Zero;
	private const float knockbackRecoverySpeed = 0.1f;

	// Damage Shake Parameters - Adjusted for gentler shake
	private const float DamageShakeMinStrength = 0.8f; // Slightly reduced (was 1.0f)
	private const float DamageShakeMaxStrength = 2.5f; // Significantly reduced (was 4.0f)
	private const float DamageShakeDuration = 0.3f;

	public event Action AudioPlayerFinished;

	public override void _Ready()
	{
		gun = GetNode<Gun>("Gun");

		if (cameraPath is not null)
		{
			camera = GetNode<ShakeyCamera>(cameraPath);
		}
		else
		{
			GD.PrintErr("Camera Path not set in Player script!");
		}

		AudioPlayerFinished += OnAudioPlayerFinished;

		if (regenTimer is not null)
		{
			regenTimer.Timeout += OnRegenTimerTimeout;
		}
		else
		{
			GD.PrintErr("RegenTimer not assigned in Player script!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, knockbackRecoverySpeed);

		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		Vector2 movement = direction * Speed + knockbackVelocity;
		Velocity = movement;
		MoveAndSlide();
	}

	public void TakeDamage(int damage)
	{
		Health -= damage;
		Health = Math.Max(Health, 0);

		damageParticles.Emitting = true;
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

		AudioStreamPlayer newAudioPlayer = new();
		audioPlayerContainer.AddChild(newAudioPlayer);

		newAudioPlayer.Stream = damageAudio;
		newAudioPlayer.Finished += () => OnSingleAudioPlayerFinished(newAudioPlayer);
		newAudioPlayer.Play();
	}

	private void OnSingleAudioPlayerFinished(AudioStreamPlayer player)
	{
		player.QueueFree();
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
		if (regenTimer is not null)
		{
			regenTimer.Stop();
		}
		QueueFree();
	}

	public void ApplyKnockback(Vector2 knockback)
	{
		knockbackVelocity = (knockbackVelocity.LengthSquared() < knockback.LengthSquared())
			? knockback
			: knockbackVelocity + knockback;
	}

	private void OnRegenTimerTimeout()
	{
		if (Health < MaxHealth)
		{
			Health = Math.Min(Health + RegenAmount, MaxHealth);
		}
	}

	public override void _ExitTree()
	{
		if (regenTimer is not null && regenTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)))
		{
			regenTimer.Timeout -= OnRegenTimerTimeout;
		}
		base._ExitTree();
	}
}
