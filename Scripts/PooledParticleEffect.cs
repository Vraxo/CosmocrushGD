using Godot;

namespace CosmocrushGD;

public partial class PooledParticleEffect : CpuParticles2D
{
    public PackedScene SourceScene { get; set; }

    [Export] private Timer returnTimer;

    public override void _Ready()
    {
        if (returnTimer is null)
        {
            GD.PrintErr($"PooledParticleEffect ({Name}): ReturnTimer not assigned! Effect will not automatically return to pool.");
            SetProcess(false);
            return;
        }

        returnTimer.Timeout += OnReturnTimerTimeout;
        Finished += OnParticlesFinished; // Also return when particles naturally finish
    }

    public void PlayEffect()
    {
        if (returnTimer is null)
        {
            GD.PrintErr($"PooledParticleEffect ({Name}): Cannot start return timer, it's null.");
            // Still emit, but won't return automatically via timer
        }
        else
        {
            returnTimer.WaitTime = Lifetime + 0.2f; // Add a slightly larger buffer
            returnTimer.Start();
        }
        Emitting = true; // Start emitting
    }

    private void OnReturnTimerTimeout()
    {
        // GD.Print($"PooledParticleEffect ({Name}): Return timer timed out."); // Optional debug
        ReturnToPool();
    }

    private void OnParticlesFinished()
    {
        // This signal fires when emitting stops AND all particles are dead
        // GD.Print($"PooledParticleEffect ({Name}): Finished signal emitted."); // Optional debug
        // We might want to return slightly before this if the timer is shorter,
        // but returning here ensures all particles are gone.
        // Stop the timer if it's still running, as we are returning now.
        returnTimer?.Stop();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (SourceScene is null || !IsInstanceValid(SourceScene))
        {
            GD.PrintErr($"PooledParticleEffect ({Name}): Cannot return to pool. SourceScene is null or invalid. Freeing.");
            QueueFree();
            return;
        }

        if (GlobalAudioPlayer.Instance is null)
        {
            GD.PrintErr($"PooledParticleEffect ({Name}): GlobalAudioPlayer instance not found. Cannot return to pool. Freeing.");
            QueueFree();
            return;
        }

        // Only return if we are not already back in the pool (e.g. timer and finished signal firing close together)
        // Check if the parent is the GlobalAudioPlayer node (where inactive particles reside)
        if (GetParent() != GlobalAudioPlayer.Instance)
        {
            // GD.Print($"PooledParticleEffect ({Name}): Returning to pool."); // Optional debug
            // *** THIS IS THE CORRECTED LINE ***
            GlobalAudioPlayer.Instance.ReturnParticleToPool(this);
        }
        else
        {
            // GD.Print($"PooledParticleEffect ({Name}): Already returned to pool, skipping."); // Optional debug
        }
    }

    public override void _ExitTree()
    {
        // Use null-conditional access and check IsConnected before disconnecting
        if (returnTimer is not null && IsInstanceValid(returnTimer))
        {
            var callable = Callable.From(OnReturnTimerTimeout);
            if (returnTimer.IsConnected(Timer.SignalName.Timeout, callable))
            {
                returnTimer.Timeout -= OnReturnTimerTimeout;
            }
            returnTimer.Stop();
        }

        var finishedCallable = Callable.From(OnParticlesFinished);
        // Check if connected before disconnecting
        if (IsConnected(SignalName.Finished, finishedCallable))
        {
            Disconnect(SignalName.Finished, finishedCallable);
        }


        base._ExitTree();
    }
}