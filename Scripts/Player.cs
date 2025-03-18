using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	public int Health = 100;
	public int MaxHealth = 100;
	public Inventory Inventory = new();

	public const float Speed = 300.0f;
	
	[Export] private Gun gun;
	[Export] private AudioStream damageAudio;
	[Export] private Sprite2D sprite;
	[Export] private CpuParticles2D damageParticles;

	private Vector2 knockbackVelocity = Vector2.Zero;
	private const float knockbackRecoverySpeed = 0.1f;
	private Node audioPlayerContainer;

	public event Action AudioPlayerFinished;

	public override void _Ready()
	{
		gun = GetNode<Gun>("Gun");

		audioPlayerContainer = new Node();
		AddChild(audioPlayerContainer);

		AudioPlayerFinished += OnAudioPlayerFinished;
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

		damageParticles.Emitting = true;
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
