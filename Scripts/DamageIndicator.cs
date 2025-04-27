using Godot;
using System;
using System.Collections.Generic;
using static System.Formats.Asn1.AsnWriter; // These seem unused, consider removing
using static System.Net.Mime.MediaTypeNames; // These seem unused, consider removing
using System.Diagnostics; // This seems unused, consider removing

namespace CosmocrushGD;

public partial class DamageIndicator : Label
{
	private static readonly Dictionary<int, string> damageStringCache = new(16);

	public int Health { get; set; } = 0;
	public int MaxHealth { get; set; } = 0;
	public PackedScene SourceScene { get; set; } // Still useful for pool manager association

	[Export] private Timer destructionTimer;
	[Export] private AnimationPlayer player;

	private const float Speed = 100;

	// Use auto-property for AnimatedAlpha
	private float animatedAlpha = 1.0f;
	public float AnimatedAlpha
	{
		get => animatedAlpha;

		set
		{
			animatedAlpha = Mathf.Clamp(value, 0f, 1f);
			UpdateAlpha();
		}
	}

	public override void _Ready()
	{
		if (destructionTimer is not null && !destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimerTimeout)))
		{
			destructionTimer.Timeout += OnTimerTimeout;
		}
		// Ensure pivot is centered for scaling animation
		PivotOffset = Size / 2;
	}

	public override void _Process(double delta)
	{
		// Early exit if disabled
		if (ProcessMode == ProcessModeEnum.Disabled)
		{
			return;
		}

		// Movement relative to global space since TopLevel = true
		float movement = Speed * (float)delta;

		GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y - movement);
	}

	public void Setup(int damageAmount, int currentHealth, int maxHealth, Vector2 globalStartPosition)
	{
		Text = GetDamageString(damageAmount);
		Health = currentHealth;
		MaxHealth = maxHealth;
		GlobalPosition = globalStartPosition; // Use GlobalPosition

		Modulate = Colors.White;
		Scale = Vector2.One;
		AnimatedAlpha = 1.0f;
		PivotOffset = Size / 2; // Recalculate pivot in case text changes size

		SetOutlineColor();

		if (player is not null)
		{
			player.Stop(true); // Reset animation state
			player.Play("DamageIndicator");
		}

		if (destructionTimer is not null)
		{
			destructionTimer.Start(); // Start or restart the timer
		}
	}

	private void SetOutlineColor()
	{
		if (MaxHealth <= 0)
		{
			AddThemeColorOverride("font_color", Colors.White);
			return;
		}

		float ratio = Mathf.Clamp((float)Health / MaxHealth, 0f, 1f);
		var outlineColor = Color.FromHsv(Mathf.Lerp(0f, 0.333f, ratio), 1f, 1f); // Green (0.333) to Red (0)

		AddThemeColorOverride("font_color", outlineColor);
	}

	private void UpdateAlpha()
	{
		// Modulate includes color and alpha
		Modulate = new(Modulate.R, Modulate.G, Modulate.B, AnimatedAlpha);
	}

	private void OnTimerTimeout()
	{
		// Use the new DamageIndicatorPoolManager
		if (DamageIndicatorPoolManager.Instance is not null)
		{
			DamageIndicatorPoolManager.Instance.ReturnIndicatorToPool(this);
		}
		else
		{
			// Fallback if the pool manager isn't available (should not happen if autoloaded)
			GD.PrintErr("DamageIndicator: DamageIndicatorPoolManager instance not found. Freeing.");
			QueueFree();
		}
	}

	// Reset state specifically for pooling
	public void ResetForPooling()
	{
		destructionTimer?.Stop();
		player?.Stop(true); // Reset animation
		// GlobalPosition is reset/set by the Setup method when reused
		// Resetting text or color overrides might be needed if they aren't always set in Setup
		Text = "";
		RemoveThemeColorOverride("font_color");
	}

	public override void _ExitTree()
	{
		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			if (destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimerTimeout)))
			{
				destructionTimer.Timeout -= OnTimerTimeout;
			}
		}
		base._ExitTree();
	}

	private static string GetDamageString(int damage)
	{
		if (damageStringCache.TryGetValue(damage, out string cachedString))
		{
			return cachedString;
		}
		else
		{
			string newString = damage.ToString();
			// Limit cache size to prevent unbounded growth
			if (damageStringCache.Count < 100) // Example limit
			{
				damageStringCache.Add(damage, newString);
			}
			return newString;
		}
	}
}
