using Godot;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;
	[Export] private AudioStream gunshotAudio;
	[Export] private Node audioPlayerContainer; // Keep using this container

	[Export] private float shakeStrength = 0.5f;
	[Export] private float shakeDuration = 0.2f;

	private ShakeyCamera camera;
	private Joystick firingJoystick;
	private Vector2 direction = Vector2.Zero;

	private const int Damage = 5;
	private const float Cooldown = 0.182f;
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 500f;

	public override void _Ready()
	{
		camera = GetNode<ShakeyCamera>("/root/World/Player/Camera2D");

		if (OS.HasFeature("mobile"))
		{
			firingJoystick = GetNode<Joystick>("/root/World/HUD/FiringJoystick");
		}

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		cooldownTimer.WaitTime = Cooldown;
	}

	public override void _Process(double delta)
	{
		Aim();
		FireIfPressed();
	}

	private void Aim()
	{
		if (OS.HasFeature("mobile"))
		{
			MobileAim();
		}
		else
		{
			DesktopAim();
		}
	}

	private void MobileAim()
	{
		if (firingJoystick == null || firingJoystick.PosVector == Vector2.Zero) return;

		LookAt(GlobalPosition + firingJoystick.PosVector);
		direction = firingJoystick.PosVector.Normalized();
	}

	private void DesktopAim()
	{
		Vector2 mousePos = GetGlobalMousePosition();
		LookAt(mousePos);
		direction = (mousePos - GlobalPosition).Normalized();
	}

	private void FireIfPressed()
	{
		bool cooledDown = cooldownTimer.IsStopped();
		bool shouldFire = false;

		if (OS.HasFeature("mobile"))
		{
			shouldFire = firingJoystick != null && firingJoystick.PosVector != Vector2.Zero;
		}
		else
		{
			shouldFire = Input.IsActionPressed("fire");
		}

		if (shouldFire && cooledDown)
		{
			Fire();
		}
	}

	private void Fire()
	{
		PlayGunshotSound();
		camera?.Shake(shakeStrength, shakeDuration);
		cooldownTimer.Start();
		DamageEnemyIfHit();
		UpdateBulletTrail();
	}

	private void PlayGunshotSound()
	{
		if (gunshotAudio == null || audioPlayerContainer == null) return;

		// Use AudioStreamPlayer instead of AudioStreamPlayer2D
		AudioStreamPlayer newAudioPlayer = new AudioStreamPlayer();
		audioPlayerContainer.AddChild(newAudioPlayer);
		newAudioPlayer.Stream = gunshotAudio;
		// Connect Finished signal for cleanup using a lambda
		newAudioPlayer.Finished += () => newAudioPlayer.QueueFree();
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
