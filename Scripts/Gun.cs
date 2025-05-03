using Godot;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;

	[Export] private float shakeStrength = 0.5f; // This is now unused here
	[Export] private float shakeDuration = 0.2f; // This is now unused here

	private ShakeyCamera camera; // Still needed if other things cause shake
	private Joystick firingJoystick;
	private Vector2 direction = Vector2.Zero;
	private AudioStream gunshotAudio;
	private Tween bulletTrailTween;

	private const int Damage = 5;
	private const float Cooldown = 0.182f; // Rate: ~5.5 shots/sec. Adjust if too fast.
	private const float BulletRange = 10000f;
	private const float KnockbackForce = 500f;
	private const float BulletTrailFadeDuration = 0.08f;

	public override void _Ready()
	{
		camera = GetParent<Player>()?.GetNode<ShakeyCamera>("Camera");

		if (OS.HasFeature("mobile"))
		{
			firingJoystick = GetNodeOrNull<Joystick>("/root/World/HUD/FiringJoystick");
		}

		rayCast.Position = Vector2.Zero;
		rayCast.TargetPosition = Vector2.Right * BulletRange;

		bulletTrail.Position = Vector2.Zero;
		bulletTrail.Visible = false;

		cooldownTimer.WaitTime = Cooldown;

		gunshotAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Gunshot.mp3");
	}

	public override void _Process(double delta)
	{
		Aim();

		if (cooldownTimer.IsStopped())
		{
			if (direction != Vector2.Zero || !OS.HasFeature("mobile"))
			{
				Fire();
			}
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
			direction = Vector2.Zero;
			return;
		}

		var lookTarget = GlobalPosition + firingJoystick.PosVector;
		LookAt(lookTarget);
		direction = firingJoystick.PosVector.Normalized();
	}

	private void DesktopAim()
	{
		var mousePos = GetGlobalMousePosition();
		LookAt(mousePos);
		direction = (mousePos - GlobalPosition).Normalized();
	}

	private void Fire()
	{
		rayCast.ForceRaycastUpdate();

		var collider = rayCast.GetCollider();
		var collisionPoint = rayCast.GetCollisionPoint();
		bool didHit = rayCast.IsColliding();

		PlayGunshotSound();
		// camera?.Shake(shakeStrength, shakeDuration); // Removed this line
		cooldownTimer.Start(); // Cooldown timer start is still here

		DamageEnemyIfHit(collider, didHit, direction);
		UpdateBulletTrail(collisionPoint, didHit);
	}

	private void PlayGunshotSound()
	{
		GlobalAudioPlayer.Instance?.PlaySound(gunshotAudio);
	}

	private void DamageEnemyIfHit(GodotObject collider, bool didHit, Vector2 knockbackDirection)
	{
		if (didHit && collider is BaseEnemy enemy)
		{
			enemy.TakeDamage(Damage);
			enemy.ApplyKnockback(knockbackDirection * KnockbackForce);
		}
	}

	private void UpdateBulletTrail(Vector2 globalCollisionPoint, bool didHit)
	{
		bulletTrail.ClearPoints();
		bulletTrail.AddPoint(Vector2.Zero);

		Vector2 localEndPoint;
		if (didHit)
		{
			localEndPoint = ToLocal(globalCollisionPoint);
		}
		else
		{
			localEndPoint = rayCast.TargetPosition;
		}

		bulletTrail.AddPoint(localEndPoint);

		bulletTrail.Modulate = Colors.White;
		bulletTrail.Visible = true;

		bulletTrailTween?.Kill();
		bulletTrailTween = CreateTween();
		bulletTrailTween.SetEase(Tween.EaseType.In);
		bulletTrailTween.SetTrans(Tween.TransitionType.Sine);
		bulletTrailTween.TweenProperty(bulletTrail, CanvasItem.PropertyName.Modulate.ToString() + ":a", 0.0f, BulletTrailFadeDuration);
	}
}
