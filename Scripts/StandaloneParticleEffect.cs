using Godot;

namespace CosmocrushGD;

public partial class StandaloneParticleEffect : CpuParticles2D
{
    [Export] private Timer autoDestructTimer;

    public override void _Ready()
    {
        if (autoDestructTimer is null)
        {
            GD.PrintErr("StandaloneParticleEffect: AutoDestructTimer not assigned! Creating one.");
            autoDestructTimer = new Timer { Name = "AutoDestructTimer", OneShot = true };
            AddChild(autoDestructTimer);
        }

        if (!autoDestructTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(SelfDestruct)))
        {
            autoDestructTimer.Timeout += SelfDestruct;
        }
    }

    public void PlayEffect()
    {
        Emitting = true;

        if (autoDestructTimer is null)
        {
            GD.PrintErr("StandaloneParticleEffect: Cannot start auto-destruct timer, it's null.");
            QueueFree();
            return;
        }

        autoDestructTimer.WaitTime = Lifetime + 0.1f;
        autoDestructTimer.Start();
    }

    private void SelfDestruct()
    {
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (autoDestructTimer is not null && IsInstanceValid(autoDestructTimer))
        {
            if (autoDestructTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(SelfDestruct)))
            {
                autoDestructTimer.Timeout -= SelfDestruct;
            }
        }

        base._ExitTree();
    }
}