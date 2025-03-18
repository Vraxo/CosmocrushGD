using Godot;
using System;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;
	[Export] private AudioStream gunshotAudio;

	// Camera shake exports
	[Export] private float shakeStrength = 0.5f;
	[Export] private float shakeDuration = 0.2f;

	private ShakeyCamera camera;
	private Node audioPlayerContainer;
	private Vector2 direction = Vector2.Zero;

	private const int Damage = 5;
	private const float Cooldown = 0.182f;
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 500f;
	
	public override void _Ready()
	{
		audioPlayerContainer = new Node();
		AddChild(audioPlayerContainer);

		// Get reference to camera
		camera = GetNode<ShakeyCamera>("/root/World/Player/Camera2D");

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		cooldownTimer.WaitTime = Cooldown;
	}

	public override void _Process(double delta)
	{
		LookAtMouse();
		FireIfPressed();
	}

	private void LookAtMouse()
	{
		Vector2 mousePosition = GetGlobalMousePosition();
		direction = (mousePosition - GlobalPosition).Normalized();
		LookAt(mousePosition);
	}

	private void FireIfPressed()
	{
		bool cooledDown = cooldownTimer.IsStopped();

		if (Input.IsActionPressed("fire") && cooledDown)
		{
			Fire();
		}
	}

	private void Fire()
	{
		PlayGunshotSound();

		// Trigger camera shake
		if (camera != null)
		{
			camera.Shake(shakeStrength, shakeDuration);
		}

		cooldownTimer.Start();
		DamageEnemyIfHit();
		UpdateBulletTrail();
	}

	private void PlayGunshotSound()
	{
		AudioStreamPlayer2D newAudioPlayer = new AudioStreamPlayer2D();
		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Stream = gunshotAudio;
		newAudioPlayer.Play();
	}

	private void PerformRayCast()
	{
		rayCast.TargetPosition = Vector2.Right * BulletRange;
		rayCast.ForceRaycastUpdate();
	}

	private void DamageEnemyIfHit()
	{
		PerformRayCast();

		if (rayCast.IsColliding() && rayCast.GetCollider() is BaseEnemy enemy)
		{
			enemy.TakeDamage(Damage);
			enemy.ApplyKnockback(direction * KnockbackForce);
		}
	}

	private void UpdateBulletTrail()
	{
		PerformRayCast();
		bulletTrail.ClearPoints();
		bulletTrail.AddPoint(Vector2.Zero);

		Vector2 endPosition = rayCast.IsColliding()
			? rayCast.GetCollisionPoint()
			: rayCast.GlobalPosition + rayCast.TargetPosition.Rotated(GlobalRotation);

		bulletTrail.AddPoint(ToLocal(endPosition));
		bulletTrail.Visible = true;
		GetTree().CreateTimer(0.1f).Timeout += () => bulletTrail.Visible = false;
	}
}
