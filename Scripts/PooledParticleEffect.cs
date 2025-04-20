using Godot;

namespace CosmocrushGD;

public partial class PooledParticleEffect : CpuParticles2D
{
    [Export] private Timer returnTimer;

    public PackedScene SourceScene { get; set; }

    public override void _Ready()
    {
        if (returnTimer is null)
        {
            GD.PrintErr("PooledParticleEffect: ReturnTimer not assigned!");
            SetProcess(false);
            return;
        }

        returnTimer.Timeout += ReturnToPool;
    }

    public void PlayEffect()
    {
        Emitting = true;

        if (returnTimer is not null)
        {
            returnTimer.WaitTime = Lifetime + 0.1f; // Add a small buffer
            returnTimer.Start();
        }
    }

    private void ReturnToPool()
    {
        if (SourceScene is not null && GlobalAudioPlayer.Instance is not null)
        {
            GlobalAudioPlayer.Instance.ReturnParticleToPool(this, SourceScene);
        }
        else
        {
            GD.PrintErr($"PooledParticleEffect: Cannot return to pool. SourceScene: {SourceScene}, GlobalAudioPlayer: {GlobalAudioPlayer.Instance}");
            QueueFree(); // Fallback if pool return fails
        }
    }

    public override void _ExitTree()
    {
        if (returnTimer is not null && IsInstanceValid(returnTimer))
        {
            returnTimer.Timeout -= ReturnToPool;
        }
        base._ExitTree();
    }
}