using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public int Health = 100;
	public int MaxHealth = 100;
	public Inventory Inventory = new();

	public const float Speed = 300.0f;
	private Gun gun;

	[Export]
	private AudioStream damageAudio;

	[Export]
	private Sprite2D sprite;

	private Vector2 knockbackVelocity = Vector2.Zero; // Knockback velocity
	private const float knockbackRecoverySpeed = 0.1f; // How quickly knockback diminishes
	private Node audioPlayerContainer;

	public event Action AudioPlayerFinished;

	public override void _Ready()
	{
		gun = GetNode<Gun>("Gun");

		audioPlayerContainer = new Node();
		AddChild(audioPlayerContainer); // Add the container to the player

		// Subscribe to the AudioPlayerFinished event
		AudioPlayerFinished += OnAudioPlayerFinished;
	}

	public override void _Process(double delta)
	{
		LookAtMouse();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Smoothly reduce the knockback velocity over time
		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, knockbackRecoverySpeed);

		// Regular movement input
		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		// Combine knockback velocity and regular movement
		Vector2 movement = direction * Speed + knockbackVelocity;

		Velocity = movement; // Apply the combined velocity
		MoveAndSlide(); // Move the player
	}

	public void TakeDamage(int damage)
	{
		Health -= damage;
		PlayDamageSound();

		if (Health <= 0)
		{
			Die();
		}
	}

	private void PlayDamageSound()
	{
		AudioStreamPlayer2D newAudioPlayer = new AudioStreamPlayer2D();
		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Stream = damageAudio;

		newAudioPlayer.Finished += () => AudioPlayerFinished?.Invoke();

		newAudioPlayer.Play();
	}

	private void OnAudioPlayerFinished()
	{
		foreach (Node child in audioPlayerContainer.GetChildren())
		{
			if (child is AudioStreamPlayer2D audioPlayer && !audioPlayer.Playing)
			{
				audioPlayer.QueueFree();
			}
		}
	}

	private void LookAtMouse()
	{
		sprite.FlipH = GlobalPosition.X > GetGlobalMousePosition().X;
	}

	private void Die()
	{
		QueueFree();
	}

	public void ApplyKnockback(Vector2 knockback)
	{
		// Apply the greater of the existing knockback or the new knockback
		if (knockbackVelocity.Length() < knockback.Length())
		{
			knockbackVelocity = knockback;
		}
		else
		{
			knockbackVelocity += knockback;
		}
	}
}
