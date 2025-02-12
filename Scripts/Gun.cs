using Godot;

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
	private float lastFiredTime = 0f;
	private float currentBloom = 0f;

	private const float Cooldown = 0.182f;
	private const float MaxBloom = 0.1f;
	private const float BloomIncrease = 0.02f;
	private const float BloomResetSpeed = 0.05f;
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 10f;
	private const float PlayerKnockbackForce = 30f;
	private const int Damage = 5;

	private const int MagazineSize = 100;
	private const float ReloadTime = 2.0f;

	private int bulletsInMagazine = MagazineSize;
	private bool reloading = false;
	private float reloadStartTime = 0f;
	private ProgressBar reloadProgressBar;

	public override void _Ready()
	{
		audioPlayerContainer = new();
		AddChild(audioPlayerContainer);
		inventory = GetNode<Inventory>("/root/World/HUD/Inventory");

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		bulletsInMagazine = MagazineSize;
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
		bool cooledDown = (Time.GetTicksMsec() / 1000f) - lastFiredTime >= Cooldown;

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
		player?.ApplyKnockback(-direction * PlayerKnockbackForce);
	}

	private void UpdateBulletTrail()
	{
		PerformRayCast();
		bulletTrail.ClearPoints();
		bulletTrail.AddPoint(Vector2.Zero);
		bulletTrail.AddPoint(ToLocal(rayCast.GetCollisionPoint()));
		bulletTrail.Visible = true;
		bulletTrail.Width = 1;
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
			float reloadProgress = (Time.GetTicksMsec() / 1000f - reloadStartTime) / ReloadTime;
			UpdateReloadProgressBar(reloadProgress);

			if (reloadProgress >= 1.0f)
			{
				FinishReloading();
			}
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
		reloadStartTime = Time.GetTicksMsec() / 1000f;
		reloadAudioPlayer.Play();
		GD.Print("Reloading...");
		CreateReloadProgressBar();
	}

	private void FinishReloading()
	{
		int availableAmmo = inventory.GetAmmo(bulletType);
		int reloadAmount = Mathf.Min(MagazineSize - bulletsInMagazine, availableAmmo);

		bulletsInMagazine += reloadAmount;
		inventory.UseAmmo(bulletType, reloadAmount);
		reloading = false;

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
