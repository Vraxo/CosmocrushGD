using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
	public int Health = 100;
	public int MaxHealth = 100;
	public Inventory PlayerInventory = new();

	public const float MovementSpeed = 300.0f;

	[Export] private Gun playerGun;
	[Export] private AudioStream damageAudio;
	[Export] private Sprite2D playerSprite;
	[Export] private CpuParticles2D damageParticles;
	[Export] private Node audioPlayerContainer;

	private Vector2 knockbackVelocity = Vector2.Zero;
	private const float KnockbackRecoverySpeed = 0.1f;

	public override void _Ready()
	{
		playerGun ??= GetNode<Gun>("Gun");
		playerSprite ??= GetNode<Sprite2D>("Sprite");
		damageParticles ??= GetNode<CpuParticles2D>("DamageParticles");
		audioPlayerContainer ??= GetNode<Node>("AudioPlayerContainer");
	}

	// Added 'void' return type
	public override void _PhysicsProcess(double delta)
	{
		knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);

		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		Vector2 targetVelocity = inputDirection * MovementSpeed + knockbackVelocity;
		Velocity = targetVelocity;

		MoveAndSlide();
	}

	public void TakeDamage(int damageAmount)
	{
		if (damageAmount <= 0)
		{
			return;
		}

		Health -= damageAmount;
		Health = Mathf.Max(Health, 0);

		if (damageParticles is not null)
		{
			damageParticles.Emitting = true;
		}

		PlayDamageSound();

		if (Health <= 0)
		{
			Die();
		}
	}

	private void PlayDamageSound()
	{
		if (damageAudio is null || audioPlayerContainer is null)
		{
			return;
		}

		AudioStreamPlayer2D audioPlayer = new()
		{
			Stream = damageAudio,
			VolumeDb = Mathf.LinearToDb(0.8f),
			Bus = "SFX"
		};

		audioPlayerContainer.AddChild(audioPlayer);
		audioPlayer.Play();
		audioPlayer.Finished += () => audioPlayer.QueueFree();
	}

	private void Die()
	{
		StatisticsManager.Instance.EndGame();

		GD.Print("Player Died!");
		QueueFree();
	}

	public void ApplyKnockback(Vector2 knockbackForce)
	{
		if (knockbackForce.LengthSquared() > knockbackVelocity.LengthSquared())
		{
			knockbackVelocity = knockbackForce;
		}
		else
		{
			knockbackVelocity += knockbackForce * 0.5f;
		}
	}
}
