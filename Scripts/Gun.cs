using Godot;

namespace CosmocrushGD;

public partial class Gun : Sprite2D
{
	[Export] private RayCast2D rayCast;
	[Export] private Line2D bulletTrail;
	[Export] private Timer cooldownTimer;
	[Export] private float shakeStrength = 0.5f; // Still unused here after previous removal
	[Export] private float shakeDuration = 0.2f; // Still unused here after previous removal

	private ShakeyCamera camera;
	private Joystick firingJoystick;
	private Vector2 direction = Vector2.Zero; // Resetting this based on aim method
	private AudioStream gunshotAudio;
	private Tween bulletTrailTween;

	private const int Damage = 5;
	private const float Cooldown = 0.182f;
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
		// RayCast target is set once, aiming is done by rotating the gun via LookAt
		rayCast.TargetPosition = Vector2.Right * BulletRange;

		bulletTrail.Position = Vector2.Zero;
		bulletTrail.Visible = false;

		cooldownTimer.WaitTime = Cooldown;

		gunshotAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Gunshot.mp3");
	}

	public override void _Process(double delta)
	{
		Aim(); // Aim first

		// Fire if cooldown is ready (consistent across platforms)
		if (cooldownTimer.IsStopped())
		{
			Fire();
		}
	}

	private void Aim()
	{
		// Aiming logic exactly as it was originally
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
		// Original MobileAim logic restored
		if (firingJoystick is null || firingJoystick.PosVector == Vector2.Zero)
		{
			direction = Vector2.Zero; // Set direction to zero if joystick is idle
			return; // Don't LookAt if joystick is idle
		}

		var lookTarget = GlobalPosition + firingJoystick.PosVector;
		LookAt(lookTarget);
		direction = firingJoystick.PosVector.Normalized();
	}

	private void DesktopAim()
	{
		// Original DesktopAim logic restored
		var mousePos = GetGlobalMousePosition();
		LookAt(mousePos);
		direction = (mousePos - GlobalPosition).Normalized();
	}

	private void Fire()
	{
		// Original Fire logic restored (mostly)
		// Raycast uses its rotation from LookAt(), no need to set TargetPosition here
		rayCast.ForceRaycastUpdate();

		var collider = rayCast.GetCollider();
		var collisionPoint = rayCast.GetCollisionPoint();
		bool didHit = rayCast.IsColliding();

		PlayGunshotSound();
		// camera?.Shake(shakeStrength, shakeDuration); // Still removed
		cooldownTimer.Start();

		// --- Knockback Direction Handling ---
		// Use the calculated 'direction' if it's non-zero (Desktop or Mobile w/ active joystick).
		// If 'direction' is zero (Mobile w/ idle joystick), use the gun's forward vector for knockback.
		Vector2 knockbackDir = direction != Vector2.Zero ? direction : Transform.X.Normalized();
		// --- End Knockback Handling ---

		DamageEnemyIfHit(collider, didHit, knockbackDir); // Use knockbackDir
		UpdateBulletTrail(collisionPoint, didHit);
	}

	private void PlayGunshotSound()
	{
		GlobalAudioPlayer.Instance?.PlaySound(gunshotAudio);
	}

	private void DamageEnemyIfHit(GodotObject collider, bool didHit, Vector2 knockbackDirection)
	{
		// Original logic using the determined knockbackDirection
		if (didHit && collider is BaseEnemy enemy)
		{
			enemy.TakeDamage(Damage);
			enemy.ApplyKnockback(knockbackDirection * KnockbackForce);
		}
	}

	private void UpdateBulletTrail(Vector2 globalCollisionPoint, bool didHit)
	{
		// Original logic restored
		bulletTrail.ClearPoints();
		bulletTrail.AddPoint(Vector2.Zero);

		Vector2 localEndPoint;
		if (didHit)
		{
			localEndPoint = ToLocal(globalCollisionPoint);
		}
		else
		{
			// Use the RayCast's target position relative to the gun's rotation
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
