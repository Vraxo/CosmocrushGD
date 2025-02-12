using Godot;
using System;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;
	[Export] private Timer reloadTimer;
	[Export] private AudioStream gunshotAudio;
	[Export] private AudioStreamPlayer reloadAudioPlayer;
	[Export] private PackedScene reloadProgressBarScene;

	private Node audioPlayerContainer;
	private Inventory inventory;
	private Vector2 direction = Vector2.Zero;
	private float currentBloom = 0f;
	private BulletType bulletType = BulletType.Medium;

	private const int Damage = 5;
	private const int MagazineSize = 100;
	private const float Cooldown = 0.182f;
	private const float MaxBloom = 0.1f;
	private const float BloomIncrease = 0.02f;
	private const float BloomResetSpeed = 0.05f;
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 10f;
	private const float PlayerKnockbackForce = 30f;
	private const float ReloadTime = 2.0f;

	private int bulletsInMagazine = MagazineSize;
	private bool reloading = false;
	private ProgressBar reloadProgressBar;

	public override void _Ready()
	{
		audioPlayerContainer = new Node();
		AddChild(audioPlayerContainer);
		inventory = GetNode<Inventory>("/root/World/HUD/Inventory");

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		cooldownTimer.WaitTime = Cooldown;
		reloadTimer.WaitTime = ReloadTime;
		reloadTimer.Timeout += FinishReloading;
	}

	public override void _Process(double delta)
	{
		LookAtMouse();

		if (!reloading)
		{
			FireIfPressed();
		}

		StopFiringIfReleased(delta);
		HandleReloading();
		TryAutoReload();
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

		if (Input.IsActionPressed("fire") && cooledDown && bulletsInMagazine > 0)
		{
			Fire();
		}
	}

	private void StopFiringIfReleased(double delta)
	{
		if (!Input.IsActionPressed("fire"))
		{
			currentBloom = Mathf.Max(0, currentBloom - BloomResetSpeed * (float)delta);
		}
	}

	private void Fire()
	{
		PlayGunshotSound();
		cooldownTimer.Start();
		DamageEnemyIfHit();
		ApplyKnockbackToPlayer();
		UpdateBulletTrail();
		IncreaseBloom();

		bulletsInMagazine--;
	}

	private void PlayGunshotSound()
	{
		AudioStreamPlayer2D newAudioPlayer = new AudioStreamPlayer2D();
		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Stream = gunshotAudio;
		newAudioPlayer.Play();
	}

	private Vector2 GetDirectionWithBloom()
	{
		float angle = (float)GD.RandRange(-currentBloom, currentBloom);
		return Vector2.Right.Rotated(angle);
	}

	private void PerformRayCast()
	{
		Vector2 finalDirection = GetDirectionWithBloom();
		rayCast.TargetPosition = finalDirection * BulletRange;
		rayCast.ForceRaycastUpdate();
	}

	private void DamageEnemyIfHit()
	{
		PerformRayCast();

		if (rayCast.IsColliding() && rayCast.GetCollider() is Enemy enemy)
		{
			enemy.TakeDamage(Damage);
			enemy.ApplyKnockback(direction * KnockbackForce);
		}
	}

	private void ApplyKnockbackToPlayer()
	{
		var player = GetParent<Player>();
		if (player == null) return;

		// Calculate direction from player's center to mouse
		Vector2 mousePosition = GetGlobalMousePosition();
		Vector2 knockbackDirection = (mousePosition - player.GlobalPosition).Normalized();
		player.ApplyKnockback(-knockbackDirection * PlayerKnockbackForce);
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

	private void IncreaseBloom()
	{
		currentBloom = Mathf.Min(MaxBloom, currentBloom + BloomIncrease);
	}

	private void HandleReloading()
	{
		if (Input.IsActionJustPressed("reload") && !reloading && bulletsInMagazine < MagazineSize)
		{
			StartReloading();
		}

		if (reloading)
		{
			float progress = 1 - (float)(reloadTimer.TimeLeft / ReloadTime);
			UpdateReloadProgressBar(progress);
		}
	}

	private void TryAutoReload()
	{
		if (bulletsInMagazine == 0 && !reloading && inventory.GetAmmo(bulletType) > 0)
		{
			StartReloading();
		}
	}

	private void StartReloading()
	{
		reloading = true;
		reloadTimer.Start();
		reloadAudioPlayer.Play();
		CreateReloadProgressBar();
	}

	private void FinishReloading()
	{
		int availableAmmo = inventory.GetAmmo(bulletType);
		int reloadAmount = Mathf.Min(MagazineSize - bulletsInMagazine, availableAmmo);

		bulletsInMagazine += reloadAmount;
		inventory.UseAmmo(bulletType, reloadAmount);
		reloading = false;

		RemoveReloadProgressBar();
	}

	private void CreateReloadProgressBar()
	{
		if (reloadProgressBarScene == null) return;

		reloadProgressBar = reloadProgressBarScene.Instantiate<ProgressBar>();
		var player = GetParent<Player>();
		player?.AddChild(reloadProgressBar);
		reloadProgressBar.Position = new Vector2(-reloadProgressBar.Size.X / 2, -60);
	}

	private void UpdateReloadProgressBar(float progress)
	{
		reloadProgressBar.Value = progress * 100;
	}

	private void RemoveReloadProgressBar()
	{
		reloadProgressBar?.QueueFree();
		reloadProgressBar = null;
	}
}
