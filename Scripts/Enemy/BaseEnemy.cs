using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

	protected static readonly Vector2 FlipThresholdVelocity = new Vector2(0.1f, 0.1f);
	private const float DefaultMeleeKnockback = 500f;
	private const float KnockbackRecovery = 5.0f;

	private const float ActiveAiUpdateInterval = 0.05f;
	private const float InactiveAiUpdateInterval = 0.25f;
	private const float InactiveMovementFraction = 0.02f;

	// Reduced distances to decrease active enemy count
	private const float ActivationDistance = 700f;
	private const float DeactivationDistance = 850f;
	private const float ActivationDistanceSq = ActivationDistance * ActivationDistance;
	private const float DeactivationDistanceSq = DeactivationDistance * DeactivationDistance;


	protected int Health;

	[Export] protected Sprite2D Sprite;
	[Export] public Timer DeathTimer { get; private set; }
	[Export] public Timer DamageCooldownTimer { get; private set; }
	[Export] public CollisionShape2D Collider;
	[Export] public AnimationPlayer HitAnimationPlayer { get; private set; }
	[Export] protected PackedScene damageParticleEffectScene;
	[Export] protected PackedScene deathParticleEffectScene;
	[Export] private AudioStream damageAudio;
	[Export] private Timer aiUpdateTimer;

	public bool Dead { get; protected set; } = false;
	public bool CanShoot { get; set; } = true;
	public Vector2 Knockback { get; set; } = Vector2.Zero;
	public Player TargetPlayer { get; set; }
	public PackedScene SourceScene { get; set; }

	protected Vector2 _currentDirectionToPlayer = Vector2.Zero;
	protected float _currentDistanceToPlayerSq = float.MaxValue;
	private bool _isPhysicsActive = true;

	protected virtual float KnockbackResistanceMultiplier => 0.5f;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f;
	protected virtual float AttackCooldown => 0.5f;
	protected virtual Color ParticleColor => Colors.White;
	protected virtual float MeleeKnockbackForce => DefaultMeleeKnockback;

	public override void _Ready()
	{
		if (DeathTimer is not null)
		{
			if (!DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
			{
				DeathTimer.Timeout += OnDeathTimerTimeout;
			}
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found!");
		}

		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			if (!DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
			{
				DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
			}
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DamageCooldownTimer node not found!");
		}

		if (aiUpdateTimer is not null)
		{
			aiUpdateTimer.WaitTime = _isPhysicsActive ? ActiveAiUpdateInterval : InactiveAiUpdateInterval;
			aiUpdateTimer.OneShot = false;
			if (!aiUpdateTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(UpdateAiPerceptionAndActivity)))
			{
				aiUpdateTimer.Timeout += UpdateAiPerceptionAndActivity;
			}
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): AiUpdateTimer node not found!");
		}
		SetPhysicsProcess(_isPhysicsActive);
		ProcessMode = _isPhysicsActive ? ProcessModeEnum.Pausable : ProcessModeEnum.Disabled;
	}

	public override void _Process(double delta)
	{
		if (Dead || !_isPhysicsActive)
		{
			return;
		}

		UpdateSpriteDirection();
		AttemptAttack();
	}

	protected virtual void UpdateAiPerceptionAndActivity()
	{
		if (Dead)
		{
			if (_isPhysicsActive) SetPhysicsActive(false);
			aiUpdateTimer?.Stop();
			return;
		}

		if (TargetPlayer != null && IsInstanceValid(TargetPlayer))
		{
			Vector2 toPlayerVector = TargetPlayer.GlobalPosition - GlobalPosition;
			_currentDistanceToPlayerSq = toPlayerVector.LengthSquared();

			if (_isPhysicsActive)
			{
				if (_currentDistanceToPlayerSq > 0.0001f)
				{
					_currentDirectionToPlayer = toPlayerVector.Normalized();
				}
				else
				{
					_currentDirectionToPlayer = Vector2.Zero;
				}

				if (_currentDistanceToPlayerSq > DeactivationDistanceSq)
				{
					SetPhysicsActive(false);
				}
			}
			else
			{
				if (_currentDistanceToPlayerSq < ActivationDistanceSq)
				{
					SetPhysicsActive(true);
					if (_currentDistanceToPlayerSq > 0.0001f)
					{
						_currentDirectionToPlayer = toPlayerVector.Normalized();
					}
					else
					{
						_currentDirectionToPlayer = Vector2.Zero;
					}
				}
				else if (_currentDistanceToPlayerSq > ActivationDistanceSq)
				{
					GlobalPosition += toPlayerVector * InactiveMovementFraction;
				}
				else if (_currentDistanceToPlayerSq > ProximityThreshold * ProximityThreshold)
				{
					GlobalPosition += toPlayerVector * InactiveMovementFraction;
				}
			}
		}
		else
		{
			_currentDirectionToPlayer = Vector2.Zero;
			_currentDistanceToPlayerSq = float.MaxValue;
			if (_isPhysicsActive)
			{
				SetPhysicsActive(false);
			}
		}
	}

	private void SetPhysicsActive(bool active)
	{
		bool currentPhysicsProcessState = GetPhysicsProcessDeltaTime() > 0;
		if (_isPhysicsActive == active && currentPhysicsProcessState == active) return;

		_isPhysicsActive = active;
		SetPhysicsProcess(active);
		ProcessMode = active ? ProcessModeEnum.Pausable : ProcessModeEnum.Disabled;

		if (aiUpdateTimer != null)
		{
			aiUpdateTimer.WaitTime = active ? ActiveAiUpdateInterval : InactiveAiUpdateInterval;
			if (!Dead && aiUpdateTimer.IsStopped())
			{
				aiUpdateTimer.Start();
			}
		}

		if (!active)
		{
			Velocity = Vector2.Zero;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta;
		float decayFactor = 1.0f - Mathf.Exp(-KnockbackRecovery * fDelta);
		Knockback = Knockback.Lerp(Vector2.Zero, decayFactor);

		if (Dead)
		{
			Velocity = Knockback;
			if (Velocity.LengthSquared() > 0.01f)
			{
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero;
				SetPhysicsProcess(false);
			}
			return;
		}

		var desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			if (_currentDistanceToPlayerSq > ProximityThreshold * ProximityThreshold)
			{
				desiredMovement = _currentDirectionToPlayer * Speed;
			}
		}

		Velocity = desiredMovement + Knockback;

		if (Velocity.LengthSquared() > 0.01f)
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero)
		{
			Velocity = Vector2.Zero;
		}
	}

	public override void _ExitTree()
	{
		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();
		aiUpdateTimer?.Stop();

		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			var callable = Callable.From(OnDeathTimerTimeout);
			if (DeathTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DeathTimer.Timeout -= OnDeathTimerTimeout;
			}
		}
		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			var callable = Callable.From(OnDamageCooldownTimerTimeout);
			if (DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
			}
		}
		if (aiUpdateTimer is not null && IsInstanceValid(aiUpdateTimer))
		{
			var callable = Callable.From(UpdateAiPerceptionAndActivity);
			if (aiUpdateTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				aiUpdateTimer.Timeout -= UpdateAiPerceptionAndActivity;
			}
		}
		base._ExitTree();
	}

	public virtual void ResetAndActivate(Vector2 position, Player player)
	{
		GlobalPosition = position;
		TargetPlayer = player;
		Health = MaxHealth;
		Dead = false;
		CanShoot = true;
		Knockback = Vector2.Zero;
		Velocity = Vector2.Zero;

		_isPhysicsActive = true;
		SetPhysicsProcess(true);
		ProcessMode = ProcessModeEnum.Pausable;
		if (aiUpdateTimer != null)
		{
			aiUpdateTimer.WaitTime = ActiveAiUpdateInterval;
		}

		UpdateAiPerceptionAndActivity();
		aiUpdateTimer?.Start();

		Visible = true;
		Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		DamageCooldownTimer?.Stop();
		DeathTimer?.Stop();
		HitAnimationPlayer?.Stop(true);
		Sprite?.Set("material:shader_parameter/flash_value", 0.0);
		Sprite.Visible = true;

		if (DeathTimer is not null && !DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
		{
			DeathTimer.Timeout += OnDeathTimerTimeout;
		}
		if (DamageCooldownTimer is not null && !DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
		}
		if (aiUpdateTimer is not null && !aiUpdateTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(UpdateAiPerceptionAndActivity)))
		{
			aiUpdateTimer.Timeout += UpdateAiPerceptionAndActivity;
		}
	}

	public virtual void ResetForPooling()
	{
		Visible = false;
		SetPhysicsActive(false);
		aiUpdateTimer?.Stop();
		if (aiUpdateTimer != null)
		{
			aiUpdateTimer.WaitTime = ActiveAiUpdateInterval;
		}

		Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		Dead = false;
		TargetPlayer = null;
		CanShoot = true;
		_currentDirectionToPlayer = Vector2.Zero;
		_currentDistanceToPlayerSq = float.MaxValue;

		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();
		HitAnimationPlayer?.Stop(true);

		if (Sprite is not null)
		{
			Sprite.Visible = true;
			Sprite.Modulate = Colors.White;
			Sprite.FlipH = false;
			Sprite.FlipV = false;
			if (Sprite.Material is ShaderMaterial shaderMat)
			{
				shaderMat.SetShaderParameter("flash_value", 0.0);
			}
		}
	}

	public void TakeDamage(int damage)
	{
		if (Dead)
		{
			return;
		}

		bool wasInactive = !_isPhysicsActive;
		if (wasInactive)
		{
			SetPhysicsActive(true);
			if (TargetPlayer != null && IsInstanceValid(TargetPlayer))
			{
				Vector2 toPlayer = TargetPlayer.GlobalPosition - GlobalPosition;
				_currentDistanceToPlayerSq = toPlayer.LengthSquared();
				_currentDirectionToPlayer = _currentDistanceToPlayerSq > 0.0001f ? toPlayer.Normalized() : Vector2.Zero;
			}
		}

		PlayDamageSound();

		Health -= damage;
		Health = Math.Max(Health, 0);
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage, Health, MaxHealth);

		var worldNode = GetNodeOrNull<World>("/root/World");
		worldNode?.AddScore(damage);

		if (damageParticleEffectScene is not null)
		{
			ParticlePoolManager.Instance?.GetParticleEffect(damageParticleEffectScene, GlobalPosition, ParticleColor);
		}

		if (Health <= 0)
		{
			Die();
		}
	}

	public void ApplyKnockback(Vector2 force)
	{
		if (Dead)
		{
			return;
		}
		if (!_isPhysicsActive)
		{
			SetPhysicsActive(true);
		}
		Knockback = force * (1.0f - float.Clamp(KnockbackResistanceMultiplier, 0f, 1f));
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null || !_isPhysicsActive)
		{
			return;
		}

		if (Mathf.Abs(Velocity.X) > FlipThresholdVelocity.X)
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}

	protected virtual void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Dead || !_isPhysicsActive)
		{
			return;
		}
		if (_currentDistanceToPlayerSq > DamageRadius * DamageRadius)
		{
			return;
		}

		PerformAttackAction();
		CanShoot = false;
		DamageCooldownTimer?.Start();
	}

	protected virtual void PerformAttackAction() { }

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}
		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);

		bool wasPreviouslyInactive = !_isPhysicsActive;
		if (wasPreviouslyInactive && Knockback.LengthSquared() > 0.01f)
		{
			SetPhysicsActive(true);
		}
		else if (wasPreviouslyInactive)
		{
			SetPhysicsProcess(false);
		}

		ProcessMode = ProcessModeEnum.Disabled;
		aiUpdateTimer?.Stop();
		if (aiUpdateTimer != null)
		{
			aiUpdateTimer.WaitTime = ActiveAiUpdateInterval;
		}

		Collider?.CallDeferred(CollisionShape2D.MethodName.SetDisabled, true);
		Sprite?.SetDeferred(Sprite2D.PropertyName.Visible, false);

		if (deathParticleEffectScene is not null)
		{
			ParticlePoolManager.Instance?.GetParticleEffect(deathParticleEffectScene, GlobalPosition, ParticleColor);
		}

		DeathTimer?.Start();
		if (DeathTimer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found! Using temporary timer to return to pool.");
			GetTree().CreateTimer(1.0).Timeout += ReturnEnemyToPool;
		}
	}

	public void ReturnEnemyToPool()
	{
		if (EnemyPoolManager.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): EnemyPoolManager instance not found. Cannot return to pool. Freeing instead.");
			QueueFree();
		}
		else
		{
			EnemyPoolManager.Instance.ReturnEnemy(this);
		}
	}

	private void ShowDamageIndicator(int damage, int currentHealth, int maxHealth)
	{
		if (DamageIndicatorPoolManager.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): DamageIndicatorPoolManager instance not found.");
			return;
		}

		DamageIndicator indicator = DamageIndicatorPoolManager.Instance.GetDamageIndicator();
		if (indicator is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Failed to get DamageIndicator from pool.");
			return;
		}

		float verticalOffset = -20f;
		if (Sprite?.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Sprite.Scale.Y - 10f;
		}
		var globalStartPosition = GlobalPosition + new Vector2(0, verticalOffset);

		indicator.Setup(damage, currentHealth, maxHealth, globalStartPosition);
	}

	private void OnDamageCooldownTimerTimeout()
	{
		CanShoot = true;
	}

	private void OnDeathTimerTimeout()
	{
		ReturnEnemyToPool();
	}

	private void PlayDamageSound()
	{
		if (damageAudio is not null)
		{
			GlobalAudioPlayer.Instance?.PlaySound2D(damageAudio, GlobalPosition);
		}
	}
}
