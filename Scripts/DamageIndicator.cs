using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class DamageIndicator : Label
{
	public int Health { get; set; } = 0;
	public int MaxHealth { get; set; } = 0;

	[Export] private Timer destructionTimer;
	[Export] private AnimationPlayer player;

	private const float Speed = 100;
	private static readonly Dictionary<int, string> damageStringCache = new(100);

	public float AnimatedAlpha
	{
		get;
		set
		{
			field = float.Clamp(value, 0f, 1f);
			UpdateAlpha();
		}
	} = 1.0f;

	public override void _Ready()
	{
		if (destructionTimer is not null && !destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimerTimeout)))
		{
			destructionTimer.Timeout += OnTimerTimeout;
		}

		PivotOffset = Size / 2;
	}

	public override void _Process(double delta)
	{
		if (ProcessMode == ProcessModeEnum.Disabled)
		{
			return;
		}

		var movement = Speed * (float)delta;

		GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y - movement);
	}

	public void Setup(int damageAmount, int currentHealth, int maxHealth, Vector2 globalStartPosition)
	{
		Text = GetDamageString(damageAmount);
		Health = currentHealth;
		MaxHealth = maxHealth;
		GlobalPosition = globalStartPosition;

		Modulate = Colors.White;
		Scale = Vector2.One;
		AnimatedAlpha = 1.0f;
		PivotOffset = Size / 2;

		SetOutlineColor();

		player?.Stop(true);
		player?.Play("DamageIndicator");

		destructionTimer?.Start();
	}

	public override void _ExitTree()
	{
		if (destructionTimer is null || !IsInstanceValid(destructionTimer))
		{
			base._ExitTree();
			return;
		}

		if (destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimerTimeout)))
		{
			destructionTimer.Timeout -= OnTimerTimeout;
		}

		base._ExitTree();
	}

	private void SetOutlineColor()
	{
		if (MaxHealth <= 0)
		{
			AddThemeColorOverride("font_color", Colors.White);
			return;
		}

		var ratio = float.Clamp((float)Health / MaxHealth, 0f, 1f);

		var outlineColor = Color.FromHsv(Mathf.Lerp(0f, 0.333f, ratio), 1f, 1f);

		AddThemeColorOverride("font_color", outlineColor);
	}

	private void UpdateAlpha()
	{
		Modulate = new(Modulate.R, Modulate.G, Modulate.B, AnimatedAlpha);
	}

	private void OnTimerTimeout()
	{
		QueueFree();
	}

	private static string GetDamageString(int damage)
	{
		if (damageStringCache.TryGetValue(damage, out var cachedString))
		{
			return cachedString;
		}

		var newString = damage.ToString();

		if (damageStringCache.Count >= 100)
		{
			return newString;
		}

		damageStringCache.Add(damage, newString);

		return newString;
	}
}
