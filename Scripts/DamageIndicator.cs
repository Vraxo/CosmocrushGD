// - Updated Setup signature to accept global position.
using Godot;
using System;
using System.Collections.Generic;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

namespace CosmocrushGD;

public partial class DamageIndicator : Label
{
	private static readonly Dictionary<int, string> damageStringCache = new(16);

	public int Health { get; set; } = 0;
	public int MaxHealth { get; set; } = 0;
	public PackedScene SourceScene { get; set; }

	[Export] private Timer destructionTimer;
	[Export] private AnimationPlayer player;

	private const float Speed = 100;

	private float _animatedAlpha = 1.0f;
	public float AnimatedAlpha
	{
		get => _animatedAlpha;
		set
		{
			_animatedAlpha = Mathf.Clamp(value, 0f, 1f);
			UpdateAlpha();
		}
	}

	public override void _Ready()
	{
		if (destructionTimer is not null && !destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimerTimeout)))
		{
			destructionTimer.Timeout += OnTimerTimeout;
		}
	}

	public override void _Process(double delta)
	{
		if (ProcessMode == ProcessModeEnum.Disabled)
		{
			return;
		}

		// Movement is now relative to global space since TopLevel = true
		float movement = Speed * (float)delta;
		GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y - movement);
	}

	// Accepts the GLOBAL position where the indicator should start
	public void Setup(int damageAmount, int currentHealth, int maxHealth, Vector2 globalStartPosition)
	{
		Text = GetDamageString(damageAmount);
		Health = currentHealth;
		MaxHealth = maxHealth;
		GlobalPosition = globalStartPosition; // Use GlobalPosition

		Modulate = Colors.White;
		Scale = Vector2.One;
		AnimatedAlpha = 1.0f;

		SetOutlineColor();

		if (player is not null)
		{
			player.Stop(true);
			player.Play("DamageIndicator");
		}

		if (destructionTimer is not null)
		{
			destructionTimer.Start();
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
		var outlineColor = Color.FromHsv(Mathf.Lerp(0f, 0.333f, ratio), 1f, 1f);
		AddThemeColorOverride("font_color", outlineColor);
	}

	private void UpdateAlpha()
	{
		Modulate = new(Modulate.R, Modulate.G, Modulate.B, AnimatedAlpha);
	}

	private void OnTimerTimeout()
	{
		if (GlobalAudioPlayer.Instance is not null)
		{
			GlobalAudioPlayer.Instance.ReturnIndicatorToPool(this);
		}
		else
		{
			GD.PrintErr("DamageIndicator: GlobalAudioPlayer instance not found. Freeing.");
			QueueFree();
		}
	}

	public void ResetForPooling()
	{
		destructionTimer?.Stop();
		player?.Stop(true);
		// Reset GlobalPosition? Maybe not necessary if Setup always sets it.
		// GlobalPosition = Vector2.Zero;
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
			if (damageStringCache.Count < 100)
			{
				damageStringCache.Add(damage, newString);
			}
			return newString;
		}
	}
}
