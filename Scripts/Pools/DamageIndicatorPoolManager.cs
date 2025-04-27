using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class DamageIndicatorPoolManager : Node
{
    public static DamageIndicatorPoolManager Instance { get; private set; }

    private PackedScene damageIndicatorScene;
    private int targetIndicatorPoolSize = 90;
    private const int IndicatorZIndex = 100;

    private Queue<DamageIndicator> availableIndicators = new();
    private bool poolsInitialized = false;
    private bool initializationStarted = false;

    public override void _EnterTree()
    {
        if (Instance is not null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public async Task InitializePoolsAsync()
    {
        if (poolsInitialized || initializationStarted)
        {
            GD.Print($"DamageIndicatorPoolManager: Initialization skipped (Initialized: {poolsInitialized}, Started: {initializationStarted})");
            return;
        }
        initializationStarted = true;
        GD.Print("DamageIndicatorPoolManager: Starting initialization...");

        damageIndicatorScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageIndicator.tscn");
        if (damageIndicatorScene is null) GD.PrintErr("DamageIndicatorPoolManager: Failed to load DamageIndicator.tscn");

        await InitializeIndicatorPoolAsync();

        poolsInitialized = true;
        initializationStarted = false;
        GD.Print("DamageIndicatorPoolManager: Initialization complete.");
    }

    private async Task InitializeIndicatorPoolAsync()
    {
        if (damageIndicatorScene is null)
        {
            GD.PrintErr("DamageIndicatorPoolManager: Cannot initialize pool: Scene is null.");
            return;
        }

        int needed = targetIndicatorPoolSize - availableIndicators.Count;
        int createdCount = 0;
        GD.Print($"DamageIndicatorPoolManager: Pool needs {needed} instances.");

        for (int i = 0; i < needed; i++)
        {
            DamageIndicator instance = CreateAndSetupIndicator();
            if (instance is not null)
            {
                availableIndicators.Enqueue(instance);
                createdCount++;
            }
            await Task.Yield(); // Allow engine processing
        }
        GD.Print($"DamageIndicatorPoolManager: - Indicator Pool: {availableIndicators.Count}/{targetIndicatorPoolSize} (Added {createdCount})");
    }

    private DamageIndicator CreateAndSetupIndicator()
    {
        if (damageIndicatorScene is null)
        {
            GD.PrintErr("DamageIndicatorPoolManager: Indicator scene is null.");
            return null;
        }
        var indicator = damageIndicatorScene.Instantiate<DamageIndicator>();
        if (indicator is null)
        {
            GD.PrintErr($"DamageIndicatorPoolManager: Failed instantiate indicator: {damageIndicatorScene.ResourcePath}");
            return null;
        }
        indicator.SourceScene = damageIndicatorScene; // Keep track of the source scene if needed
        indicator.TopLevel = true; // Make independent of parent transform
        indicator.Visible = false; // Start invisible
        indicator.ProcessMode = ProcessModeEnum.Disabled; // Start disabled
        indicator.ZIndex = IndicatorZIndex;
        AddChild(indicator); // Add to the manager node
        return indicator;
    }

    public DamageIndicator GetDamageIndicator()
    {
        if (!poolsInitialized)
        {
            GD.PushWarning("DamageIndicatorPoolManager.GetDamageIndicator called before pools fully initialized!");
            var emergencyIndicator = CreateAndSetupIndicator();
            if (emergencyIndicator is not null)
            {
                GD.PrintErr("DamageIndicatorPoolManager: Returning emergency indicator instance.");
                // Needs manual setup if used before pool ready, mimicking parts of SetupIndicatorInstance
                emergencyIndicator.Visible = true;
                emergencyIndicator.ProcessMode = ProcessModeEnum.Pausable;
                emergencyIndicator.Modulate = Colors.White;
                emergencyIndicator.AnimatedAlpha = 1.0f;
                emergencyIndicator.Scale = Vector2.One;
            }
            return emergencyIndicator;
        }

        DamageIndicator indicator;
        if (availableIndicators.Count > 0)
        {
            indicator = availableIndicators.Dequeue();
            if (indicator is null || !IsInstanceValid(indicator))
            {
                GD.PrintErr("DamageIndicatorPoolManager: Invalid indicator in pool. Creating replacement.");
                indicator = CreateAndSetupIndicator();
                if (indicator is null) return null;
            }
        }
        else
        {
            GD.Print("DamageIndicatorPoolManager: Indicator pool empty! Creating new instance.");
            indicator = CreateAndSetupIndicator();
            if (indicator is null) return null;
        }

        SetupIndicatorInstance(indicator);
        return indicator;
    }

    private void SetupIndicatorInstance(DamageIndicator indicator)
    {
        indicator.Visible = true;
        indicator.ProcessMode = ProcessModeEnum.Pausable; // Enable processing, respecting pause
        indicator.Modulate = Colors.White; // Reset visual state
        indicator.AnimatedAlpha = 1.0f;
        indicator.Scale = Vector2.One;
        // ZIndex is set during CreateAndSetupIndicator
        // The actual text, position, health ratio etc. is set by the caller via indicator.Setup(...)
    }


    public void ReturnIndicatorToPool(DamageIndicator indicator)
    {
        if (indicator is null || !IsInstanceValid(indicator))
        {
            GD.PrintErr($"DamageIndicatorPoolManager.ReturnIndicatorToPool: Invalid indicator instance {indicator?.GetInstanceId()}.");
            return;
        }

        // Reset state before returning
        indicator.Visible = false;
        indicator.ProcessMode = ProcessModeEnum.Disabled;
        indicator.ResetForPooling(); // Call the indicator's own reset method

        availableIndicators.Enqueue(indicator);
    }

    public void CleanUpActiveObjects()
    {
        GD.Print("DamageIndicatorPoolManager: Cleaning up active indicators...");
        var nodesToClean = new List<DamageIndicator>();

        foreach (Node child in GetChildren())
        {
            if (child is DamageIndicator indicator && indicator.ProcessMode != ProcessModeEnum.Disabled)
            {
                nodesToClean.Add(indicator);
            }
        }

        GD.Print($"DamageIndicatorPoolManager: Found {nodesToClean.Count} active indicators to clean.");

        foreach (var indicator in nodesToClean)
        {
            if (IsInstanceValid(indicator))
            {
                GD.Print($" - Returning active indicator {indicator.GetInstanceId()} to pool.");
                ReturnIndicatorToPool(indicator);
            }
        }
        GD.Print("DamageIndicatorPoolManager: Finished cleaning active indicators.");
    }
}