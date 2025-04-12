using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	public int Health = 100;
	public int MaxHealth = 100;
	public Inventory Inventory = new();

	public const float Speed = 300.0f;
	private const int RegenAmount = 1; // Amount to regenerate per tick

	[Export] private Gun gun;
	[Export] private AudioStream damageAudio;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private Node audioPlayerContainer; // Keep this for organization
	[Export] private Timer regenTimer;

	private Vector2 knockbackVelocity = Vector2.Zero;
	private const float knockbackRecoverySpeed = 0.1f;

	public event Action AudioPlayerFinished;

	public override void _Ready()
	{
		gun = GetNode<Gun>("Gun");

		AudioPlayerFinished += OnAudioPlayerFinished;

		if (regenTimer != null)
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
		Health = Math.Max(Health, 0); // Ensure health doesn't go below 0

		damageParticles.Emitting = true;
		PlayDamageSound();

		if (Health <= 0)
		{
			Die();
		}
	}

	private void PlayDamageSound()
	{
		if (damageAudio == null || audioPlayerContainer == null) return;

		// Use AudioStreamPlayer instead of AudioStreamPlayer2D
		AudioStreamPlayer newAudioPlayer = new();
		audioPlayerContainer.AddChild(newAudioPlayer);

		newAudioPlayer.Stream = damageAudio;
		// Connect Finished signal for cleanup using a lambda
		newAudioPlayer.Finished += () => OnSingleAudioPlayerFinished(newAudioPlayer);
		newAudioPlayer.Play();
	}

	// Simplified cleanup for single players
	private void OnSingleAudioPlayerFinished(AudioStreamPlayer player)
	{
		player.QueueFree();
		// No need to invoke the old event system here unless other things relied on it
	}

	// Kept old handler in case it's needed, but the new sounds use the lambda above
	private void OnAudioPlayerFinished()
	{
		foreach (Node child in audioPlayerContainer.GetChildren())
		{
			// Check for both types just in case, but new ones are AudioStreamPlayer
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
		if (regenTimer != null)
		{
			regenTimer.Stop();
		}
		QueueFree();
	}

	public void ApplyKnockback(Vector2 knockback)
	{
		knockbackVelocity = (knockbackVelocity.Length() < knockback.Length())
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
		if (regenTimer != null && regenTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)))
		{
			regenTimer.Timeout -= OnRegenTimerTimeout;
		}
		// Consider cleaning up remaining audio players in the container if the scene might exit abruptly
		base._ExitTree();
	}
}
