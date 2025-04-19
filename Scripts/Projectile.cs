using System.Xml.Linq;
using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;
	[Export] public Timer LifetimeTimer;
	[Export] public Timer DestructionDelayTimer;

	private bool active = true;

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DefaultDestructionDelay = 1.0f;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		if (LifetimeTimer is not null)
		{
			LifetimeTimer.WaitTime = DefaultLifetime;
			LifetimeTimer.Timeout += OnLifetimeTimeout;
			LifetimeTimer.Start();
		}
		else
		{
		}

		if (DestructionDelayTimer is not null)
		{
			DestructionDelayTimer.WaitTime = DefaultDestructionDelay;
			DestructionDelayTimer.OneShot = true;
			DestructionDelayTimer.Timeout += QueueFreeSelf;
		}
		else
		{
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}

		GlobalPosition += Direction * Speed * (float)delta;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(this))
		{
			BodyEntered -= OnBodyEntered;
		}
		if (LifetimeTimer is not null && IsInstanceValid(LifetimeTimer))
		{
			LifetimeTimer.Timeout -= OnLifetimeTimeout;
		}
		if (DestructionDelayTimer is not null && IsInstanceValid(DestructionDelayTimer))
		{
			DestructionDelayTimer.Timeout -= QueueFreeSelf;
		}
		base._ExitTree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!active || body is not Player player)
		{
			return;
		}

		player.TakeDamage(1);
		player.ApplyKnockback(Direction * KnockbackForce);
		StartDestructionSequence();
	}

	private void OnLifetimeTimeout()
	{
		if (!active)
		{
			return;
		}
		StartDestructionSequence();
	}

	private void StartDestructionSequence()
	{
		if (!active)
		{
			return;
		}

		active = false;
		LifetimeTimer?.Stop();

		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);

		if (Sprite != null)
		{
			Sprite.Visible = false;
		}

		Direction = Vector2.Zero;

		if (DestructionParticles is not null)
		{
			DestructionParticles.OneShot = true;
		}

		if (DestructionDelayTimer is not null)
		{
			DestructionDelayTimer.Start();
		}
		else
		{
			QueueFreeSelf();
		}
	}

	private void QueueFreeSelf()
	{
		if (!IsInstanceValid(this))
		{
			return;
		}
		QueueFree();
	}
}
