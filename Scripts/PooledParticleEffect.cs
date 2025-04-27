using Godot;

namespace CosmocrushGD;

public partial class PooledParticleEffect : CpuParticles2D
{
    // SourceScene is still needed to know which pool queue to return to
    public PackedScene SourceScene { get; set; }

    [Export] private Timer returnTimer;

    public override void _Ready()
    {
        if (returnTimer is null)
        {
            GD.PrintErr("PooledParticleEffect: ReturnTimer not assigned!");
            SetProcess(false); // Disable if timer is missing
            return;
        }

        // Connect only once
        if (!returnTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
        {
            returnTimer.Timeout += ReturnToPool;
        }
    }

    public void PlayEffect()
    {
        Emitting = true; // Start emitting particles

        if (returnTimer is null)
        {
            GD.PrintErr("PooledParticleEffect: Cannot start return timer, it's null.");
            // Consider self-destruct or logging an error
            return;
        }

        returnTimer.WaitTime = Lifetime + 0.1f; // Use particle lifetime plus a buffer
        returnTimer.Start();
    }

    private void ReturnToPool()
    {
        // Use the new ParticlePoolManager
        if (ParticlePoolManager.Instance is not null)
        {
            ParticlePoolManager.Instance.ReturnParticleToPool(this);
        }
        else
        {
            GD.PrintErr($"PooledParticleEffect: Cannot return to pool. ParticlePoolManager instance not found. Freeing particle.");
            QueueFree(); // Fallback if pool return fails
        }
    }

    public override void _ExitTree()
    {
        // Ensure disconnection if the node is valid and the signal is connected
        if (returnTimer is not null && IsInstanceValid(returnTimer))
        {
            if (returnTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
            {
                returnTimer.Timeout -= ReturnToPool;
            }
        }

        base._ExitTree();
    }
}