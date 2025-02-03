using Godot;
using static Godot.TextServer;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private AudioStream gunshotAudio;
	[Export] private AudioStreamPlayer reloadAudioPlayer;
	[Export] private PackedScene reloadProgressBarScene;
	[Export] private BulletType bulletType = BulletType.Medium;

	private Node audioPlayerContainer;
	private Inventory inventory;
	private Vector2 direction = Vector2.Zero;
	private float cooldown = 0.182f;
	private float lastFiredTime = 0f;
	private float currentBloom = 0f;
	private const float maxBloom = 0.1f;
	private const float bloomIncrease = 0.02f;
	private const float bloomResetSpeed = 0.05f;
	private const float bulletRange = 10000f;
	private const float knockbackForce = 10f;
	private const float playerKnockbackForce = 30f;
	private const int damage = 5;

	[Export] private int magazineSize = 10;
	[Export] private float reloadTime = 2.0f;

	private int bulletsInMagazine;
	private bool isReloading = false;
	private float reloadStartTime = 0f;
	private ProgressBar reloadProgressBar;

	public override void _Ready()
	{
		audioPlayerContainer = new();
		AddChild(audioPlayerContainer);
		inventory = GetNode<Inventory>("/root/World/HUD/Inventory");

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		bulletsInMagazine = magazineSize;
	}

	public override void _Process(double delta)
	{
		LookAtMouse();

		if (!isReloading)
		{
			FireIfPressed();
		}

		StopFiringIfReleased(delta);
		HandleReloading(delta);
		TryAutoReload();
	}

	private void LookAtMouse()
	{
		Vector2 mousePosition = GetGlobalMousePosition();
		direction = (mousePosition - GlobalPosition).Normalized();
		LookAt(mousePosition);
		FlipV = mousePosition.X < GlobalPosition.X;
	}

	private void FireIfPressed()
	{
		bool cooledDown = (Time.GetTicksMsec() / 1000f) - lastFiredTime >= cooldown;

		if (Input.IsActionPressed("fire") && cooledDown && bulletsInMagazine > 0)
		{
			Fire();
		}
	}

	private void StopFiringIfReleased(double delta)
	{
		if (!Input.IsActionPressed("fire"))
		{
			currentBloom = Mathf.Max(0, currentBloom - bloomResetSpeed * (float)delta);
		}
	}

	private void Fire()
	{
		PlayGunshotSound();
		SetLastFiredTime();
		DamageEnemyIfHit();
		ApplyKnockbackToPlayer();
		UpdateBulletTrail();
		IncreaseBloom();

		bulletsInMagazine--;
		GD.Print("Bullets left in magazine: " + bulletsInMagazine);
	}

	private void PlayGunshotSound()
	{
		AudioStreamPlayer2D newAudioPlayer = new();
		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Stream = gunshotAudio;
		newAudioPlayer.Play();
	}

	private void SetLastFiredTime()
	{
		lastFiredTime = Time.GetTicksMsec() / 1000f;
	}

	private Vector2 GetDirectionWithBloom()
	{
		if (currentBloom == 0f)
			return Vector2.Right;

		float angle = (float)GD.RandRange(-currentBloom, currentBloom);
		return Vector2.Right.Rotated(angle);
	}

	private void PerformRaycast()
	{
		Vector2 finalDirection = GetDirectionWithBloom();
		rayCast.TargetPosition = finalDirection * bulletRange;
		rayCast.ForceRaycastUpdate();
	}

	private void DamageEnemyIfHit()
	{
		PerformRaycast();

		if (rayCast.IsColliding() && rayCast.GetCollider() is Area2D hitbox)
		{
			GD.Print("Hit!");
			var enemy = hitbox.GetParent<Enemy>();
			enemy.TakeDamage(damage);
			enemy.ApplyKnockback(direction * knockbackForce);
		}
	}

	private void ApplyKnockbackToPlayer()
	{
		var player = GetParent<Player>();
		player?.ApplyKnockback(-direction * playerKnockbackForce);
	}

	private void UpdateBulletTrail()
	{
		PerformRaycast();
		bulletTrail.ClearPoints();
		bulletTrail.AddPoint(Vector2.Zero);
		bulletTrail.AddPoint(ToLocal(rayCast.GetCollisionPoint()));
		bulletTrail.Visible = true;
		GetTree().CreateTimer(0.1f).Timeout += () => bulletTrail.Visible = false;
	}

	private void IncreaseBloom()
	{
		currentBloom = Mathf.Min(maxBloom, currentBloom + bloomIncrease);
	}

	private void HandleReloading(double delta)
	{
		if (Input.IsActionJustPressed("reload") && !isReloading && bulletsInMagazine < magazineSize)
		{
			StartReloading();
		}

		if (isReloading)
		{
			float reloadProgress = (Time.GetTicksMsec() / 1000f - reloadStartTime) / reloadTime;
			UpdateReloadProgressBar(reloadProgress);

			if (reloadProgress >= 1.0f)
			{
				FinishReloading();
			}
		}
	}

	private void TryAutoReload()
	{
		if (bulletsInMagazine == 0 && !isReloading && inventory.GetAmmo(bulletType) > 0)
		{
			StartReloading();
		}
	}

	private void StartReloading()
	{
		isReloading = true;
		reloadStartTime = Time.GetTicksMsec() / 1000f;
		reloadAudioPlayer.Play();
		GD.Print("Reloading...");
		CreateReloadProgressBar();
	}

	private void FinishReloading()
	{
		int availableAmmo = inventory.GetAmmo(bulletType);
		int reloadAmount = Mathf.Min(magazineSize - bulletsInMagazine, availableAmmo);

		bulletsInMagazine += reloadAmount;
		inventory.UseAmmo(bulletType, reloadAmount);
		isReloading = false;

		GD.Print($"Reloaded {reloadAmount} bullets. Magazine: {bulletsInMagazine}");
		RemoveReloadProgressBar();
	}

	private void CreateReloadProgressBar()
	{
		if (reloadProgressBarScene == null) return;

		reloadProgressBar = reloadProgressBarScene.Instantiate<ProgressBar>();
		var player = GetParent<Player>();
		player?.AddChild(reloadProgressBar);
		reloadProgressBar.Position = new(-reloadProgressBar.Size.X / 2, -60);
	}

	private void UpdateReloadProgressBar(float progress)
	{
		reloadProgressBar?.SetValue(progress * 100);
	}

	private void RemoveReloadProgressBar()
	{
		reloadProgressBar?.QueueFree();
		reloadProgressBar = null;
	}
}
