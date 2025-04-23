using Godot;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;


	[Export] private float shakeStrength = 0.5f;
	[Export] private float shakeDuration = 0.2f;

	private ShakeyCamera camera;
	private Joystick firingJoystick;
	private Vector2 direction = Vector2.Zero;
	private AudioStream gunshotAudio;

	private const int Damage = 5;
	private const float Cooldown = 0.182f;
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 500f;


	public override void _Ready()
	{
		camera = GetParent<Player>().GetNode<ShakeyCamera>("Camera");

		if (OS.HasFeature("mobile"))
		{
			firingJoystick = GetNode<Joystick>("/root/World/HUD/FiringJoystick");
		}

		rayCast.Position = Vector2.Zero;
		bulletTrail.Position = Vector2.Zero;

		cooldownTimer.WaitTime = Cooldown;


		gunshotAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Gunshot.mp3");
	}

	public override void _Process(double delta)
	{
		Aim();

		if (cooldownTimer.IsStopped())
		{
			Fire();
		}
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
		if (firingJoystick is null || firingJoystick.PosVector == Vector2.Zero)
		{
			return;
		}

		LookAt(GlobalPosition + firingJoystick.PosVector);
		direction = firingJoystick.PosVector.Normalized();
	}

	private void DesktopAim()
	{
		Vector2 mousePos = GetGlobalMousePosition();
		LookAt(mousePos);
		direction = (mousePos - GlobalPosition).Normalized();
	}

	private void Fire()
	{
		//PlayGunshotSound();
		//camera.Shake(shakeStrength, shakeDuration);
		cooldownTimer.Start();
		DamageEnemyIfHit();
		UpdateBulletTrail();
	}

	private void PlayGunshotSound()
	{
		if (gunshotAudio is not null && GlobalAudioPlayer.Instance is not null)
		{
			GlobalAudioPlayer.Instance.PlaySound(gunshotAudio);
		}
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
