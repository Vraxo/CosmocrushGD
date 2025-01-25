using Godot;

public partial class Enemy : CharacterBody2D
{
	[Export] private NavigationAgent2D navigationAgent2D;
	[Export] private Sprite2D sprite;
	[Export] private PackedScene damageIndicatorScene;
	[Export] private Area2D hitBoxArea;
	[Export] private Timer deathTimer;

	private bool dead = false;
	private Player player;
	private const float damageRadius = 50f;
	private const float damageCooldown = 0.5f;
	private double lastDamageTime = -damageCooldown;
	private const float Speed = 100.0f;
	private int health = 20;
	private const int maxHealth = 20;
	private Vector2 knockback = Vector2.Zero;           // Knockback velocity
	private const float knockbackRecoverySpeed = 0.1f; // How fast the knockback diminishes

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
		knockback = knockback.Lerp(Vector2.Zero, knockbackRecoverySpeed);

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

		navigationAgent2D.TargetPosition = player.GlobalPosition;

		//if (navigationAgent2D.GetNextPathPosition() is null)
		//{
		//	return Vector2.Zero;
		//}

		return (navigationAgent2D.GetNextPathPosition() - GlobalPosition).Normalized();
	}

	private void AttemptToDamagePlayer()
	{
		if (player is null)
		{
			return;
		}

		bool isPlayerInRange = GlobalPosition.DistanceTo(player.GlobalPosition) <= damageRadius;
		bool canShoot = Time.GetTicksMsec() / 1000.0 - lastDamageTime >= damageCooldown;

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
		damageIndicator.MaxHealth = maxHealth;
		damageIndicator.Position = new(damageIndicator.Position.X, damageIndicator.Position.Y - ((sprite.Texture.GetSize().Y * sprite.Scale.Y) / 2));
		AddChild(damageIndicator);
	}

	private void Die()
	{
		dead = true;
		deathTimer.Start();
		sprite.Visible = false;
		hitBoxArea.QueueFree();
	}

	private void OnDeathTimerTimeOut()
	{
		QueueFree();
	}
}
