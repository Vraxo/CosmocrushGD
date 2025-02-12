using Godot;

namespace CosmocrushGD;

public partial class Enemy : CharacterBody2D
{
	[Export] private NavigationAgent2D navigator;
	[Export] private Sprite2D sprite;
	[Export] private Timer deathTimer;
	[Export] private CollisionShape2D collider;
	[Export] private PackedScene damageIndicatorScene;

	private bool dead = false;
	private int health = 20;
	private double lastDamageTime = -DamageCooldown;
	private Vector2 knockback = Vector2.Zero;           // Knockback velocity
	private Player player;
	
	private const float DamageRadius = 50f;
	private const float DamageCooldown = 0.5f;
	private const float Speed = 100.0f;
	private const int MaxHealth = 20;
	private const float KnockbackRecoverySpeed = 0.1f; // How fast the knockback diminishes

	// Main

	public override void _Ready()
	{
		player = GetNode<Player>("/root/World/Player");
		deathTimer.Timeout += OnDeathTimerTimeOut;
	}

	public override void _Process(double delta)
	{
		if (dead)
		{
			return;
		}

		LookAtPlayer();
		AttemptToDamagePlayer();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (dead)
		{
			return;
		}

		// Smoothly reduce the knockback velocity
		knockback = knockback.Lerp(Vector2.Zero, KnockbackRecoverySpeed);

		// Movement logic (chase player)
		Vector2 movement = GetChaseDirection() * Speed * (float)delta + knockback;

		Velocity = movement / (float)delta; // Set Velocity for MoveAndSlide compatibility
		MoveAndSlide();
	}

	// Public

	public void TakeDamage(int damage)
	{
		health -= damage;

		GetNode<AnimationPlayer>("Sprite/AnimationPlayer").Play("new_animation");

		CreateDamageIndicator(damage);

		if (health <= 0)
		{
			Die();
		}
	}

	public void ApplyKnockback(Vector2 force)
	{
		if (knockback.Length() < force.Length())
		{
			knockback = force;
		}
		else
		{
			knockback += force;
		}
	}

	private void LookAtPlayer()
	{
		if (player is null)
		{
			return;
		}

		sprite.FlipH = GlobalPosition.X < player.GlobalPosition.X;
	}

	private Vector2 GetChaseDirection()
	{
		if (player is null)
		{
			return Vector2.Zero;
		}

		navigator.TargetPosition = player.GlobalPosition;

		//if (navigator.GetNextPathPosition() is null)
		//{
		//	return Vector2.Zero;
		//}

		return (navigator.GetNextPathPosition() - GlobalPosition).Normalized();
	}

	private void AttemptToDamagePlayer()
	{
		if (player is null)
		{
			return;
		}

		bool isPlayerInRange = GlobalPosition.DistanceTo(player.GlobalPosition) <= DamageRadius;
		bool canShoot = Time.GetTicksMsec() / 1000.0 - lastDamageTime >= DamageCooldown;

		if (isPlayerInRange && canShoot)
		{
			player?.TakeDamage(1);
			lastDamageTime = Time.GetTicksMsec() / 1000.0;
		}
	}

	private void CreateDamageIndicator(int damage)
	{
		var damageIndicator = damageIndicatorScene.Instantiate<DamageIndicator>();
		damageIndicator.Text = damage.ToString();
		damageIndicator.Health = health;
		damageIndicator.MaxHealth = MaxHealth;
		damageIndicator.Position = new(0, -(sprite.Texture.GetSize().Y * sprite.Scale.Y / 2));
		AddChild(damageIndicator);
	}

	private void Die()
	{
		dead = true;
		collider.Disabled = true;
		deathTimer.Start();
		sprite.Visible = false;
		
	}

	private void OnDeathTimerTimeOut()
	{
		QueueFree();
	}
}
