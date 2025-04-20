using Godot;
using System;

namespace CosmocrushGD;

public partial class Player : CharacterBody2D
{
    public int Health = 100;
    public int MaxHealth = 100;
    public Inventory Inventory = new();

    [Export] private Gun gun;
    [Export] private Sprite2D sprite;
    [Export] private CpuParticles2D damageParticles;
    [Export] private CpuParticles2D deathParticles;
    [Export] private Timer regenerationTimer;
    [Export] private NodePath cameraPath;
    [Export] private Timer deathPauseTimer;
    [Export] private AudioStreamPlayer deathAudioPlayer;

    private ShakeyCamera camera;
    private Vector2 knockbackVelocity = Vector2.Zero;
    private AudioStream damageAudio;

    private const int RegenerationRate = 0;
    private const float Speed = 300.0f;
    private const float KnockbackRecoverySpeed = 0.1f;
    private const float DamageShakeMinStrength = 0.8f;
    private const float DamageShakeMaxStrength = 2.5f;
    private const float DamageShakeDuration = 0.3f;
    private const float DesktopDeathZoomAmount = 2.0f;
    private const float MobileDeathZoomAmount = 3.0f;
    private const float DeathZoomDuration = 1.5f;

    public event Action GameOver;

    public override void _Ready()
    {
        if (cameraPath is not null)
        {
            camera = GetNode<ShakeyCamera>(cameraPath);
            if (IsInstanceValid(camera))
            {
                camera.ResetZoom();
            }
            else
            {
                camera = null;
            }
        }
        else
        {
        }

        if (regenerationTimer is not null)
        {
            regenerationTimer.Timeout += OnRegenTimerTimeout;
        }
        else
        {
        }

        if (deathPauseTimer is not null)
        {
            deathPauseTimer.Timeout += OnDeathPauseTimerTimeout;
        }
        else
        {
        }


        damageAudio = ResourceLoader.Load<AudioStream>("res://Audio/SFX/PlayerDamage.mp3");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || (deathPauseTimer is not null && !deathPauseTimer.IsStopped()))
        {
            return;
        }

        knockbackVelocity = knockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);

        Vector2 direction = Input.GetVector("left", "right", "up", "down");
        Vector2 movement = direction * Speed + knockbackVelocity;
        Velocity = movement;
        MoveAndSlide();
    }

    public override void _ExitTree()
    {
        if (regenerationTimer is not null && regenerationTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnRegenTimerTimeout)))
        {
            regenerationTimer.Timeout -= OnRegenTimerTimeout;
        }
        if (deathPauseTimer is not null && deathPauseTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathPauseTimerTimeout)))
        {
            deathPauseTimer.Timeout -= OnDeathPauseTimerTimeout;
        }

        base._ExitTree();
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0) return;

        Health -= damage;
        Health = Math.Max(Health, 0);

        if (damageParticles is not null)
        {
            damageParticles.Emitting = true;
        }

        PlayDamageSound();
        TriggerDamageShake();

        if (Health <= 0)
        {
            Die();
        }
    }

    private void TriggerDamageShake()
    {
        if (camera is null || !IsInstanceValid(camera))
        {
            return;
        }

        float healthRatio = MaxHealth > 0
            ? Mathf.Clamp((float)Health / MaxHealth, 0f, 1f)
            : 0f;

        float shakeStrength = Mathf.Lerp(DamageShakeMaxStrength, DamageShakeMinStrength, healthRatio);

        camera.Shake(shakeStrength, DamageShakeDuration);
    }

    private void PlayDamageSound()
    {
        if (damageAudio is not null && GlobalAudioPlayer.Instance is not null)
        {
            GlobalAudioPlayer.Instance.PlaySound(damageAudio);
        }
    }

    private void Die()
    {
        regenerationTimer?.Stop();
        ProcessMode = ProcessModeEnum.Disabled;
        SetPhysicsProcess(false);

        if (sprite is not null)
        {
            sprite.Visible = false;
        }
        if (gun is not null)
        {
            gun.Visible = false;
        }
        if (deathParticles is not null)
        {
            deathParticles.Emitting = true;
        }

        if (deathAudioPlayer is not null)
        {
            deathAudioPlayer.Play();
        }

        if (camera is not null)
        {
            float zoomAmount = OS.HasFeature("mobile")
                ? MobileDeathZoomAmount
                : DesktopDeathZoomAmount;
            camera.ZoomToPoint(zoomAmount, DeathZoomDuration);
        }

        if (deathPauseTimer is not null)
        {
            deathPauseTimer.Start();
        }
    }

    private void OnDeathPauseTimerTimeout()
    {
        if (!GetTree().Paused)
        {
            GetTree().Paused = true;
        }

        GameOver?.Invoke();
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        knockbackVelocity = knockbackVelocity.LengthSquared() < knockback.LengthSquared()
            ? knockback
            : knockbackVelocity + knockback;
    }

    private void OnRegenTimerTimeout()
    {
        if (Health >= MaxHealth)
        {
            return;
        }

        Health = Math.Min(Health + RegenerationRate, MaxHealth);
    }
}