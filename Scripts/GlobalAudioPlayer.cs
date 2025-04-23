using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Keep for Task used in Initialize method

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
    // Instance and Basic Properties
    public static GlobalAudioPlayer Instance { get; private set; }
    public AudioStream UiSound { get; private set; }

    // Resource Scenes (Loaded on demand during initialization)
    private PackedScene damageParticleScene;
    private PackedScene deathParticleScene;
    private PackedScene damageIndicatorScene;
    private PackedScene defaultProjectileScene;

    // Pool Target Sizes
    private int targetParticlePoolSize = 60;
    private int targetIndicatorPoolSize = 90;
    private int targetProjectilePoolSize = 60;
    private const int TargetAudioPoolSize = 10;

    // Constants
    private const string SfxBusName = "SFX";
    private const int ParticleZIndex = 10;

    // Pool Queues
    private Queue<AudioStreamPlayer> availablePlayers1D = new();
    private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
    private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();
    private Queue<DamageIndicator> availableIndicators = new();
    private Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();

    // Initialization State Flag
    private bool gameplayPoolsInitialized = false;
    private bool initializationStarted = false;


    public override void _EnterTree()
    {
        if (Instance is not null)
        {
            GD.Print("GlobalAudioPlayer: Instance already exists, freeing new one.");
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("GlobalAudioPlayer: Instance created.");

        LoadMinimalResources();
        InitializeMinimalPools();
    }

    private void LoadMinimalResources()
    {
        UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");
        if (UiSound is null)
        {
            GD.PrintErr("Failed to load UiSound");
        }
    }

    private void InitializeMinimalPools()
    {
        GD.Print("GlobalAudioPlayer: Initializing minimal (audio) pools...");
        for (int i = 0; i < TargetAudioPoolSize; i++)
        {
            availablePlayers1D.Enqueue(CreateAndSetupPlayer1D());
            availablePlayers2D.Enqueue(CreateAndSetupPlayer2D());
        }
        GD.Print($"- Audio Pools: {availablePlayers1D.Count}/{TargetAudioPoolSize}");
    }

    public async Task InitializeGameplayPoolsAsync()
    {
        if (gameplayPoolsInitialized || initializationStarted)
        {
            GD.Print($"GlobalAudioPlayer: Gameplay pool initialization skipped (Already Initialized: {gameplayPoolsInitialized}, Already Started: {initializationStarted})");
            return;
        }
        initializationStarted = true;
        GD.Print("GlobalAudioPlayer: Starting gameplay pools initialization (Particles, Indicators, Projectiles)...");


        damageParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageParticleEffect.tscn");
        deathParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DeathParticleEffect.tscn");
        damageIndicatorScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageIndicator.tscn");
        defaultProjectileScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");

        if (damageParticleScene is null) GD.PrintErr("Failed to load DamageParticleEffect.tscn");
        if (deathParticleScene is null) GD.PrintErr("Failed to load DeathParticleEffect.tscn");
        if (damageIndicatorScene is null) GD.PrintErr("Failed to load DamageIndicator.tscn");
        if (defaultProjectileScene is null) GD.PrintErr("Failed to load default Projectile.tscn");


        int indicatorsCreated = 0;
        if (damageIndicatorScene is not null)
        {
            if (!availableParticles.ContainsKey(damageIndicatorScene))
            {
                availableIndicators = new Queue<DamageIndicator>(targetIndicatorPoolSize);
            }

            for (int i = 0; i < targetIndicatorPoolSize; i++)
            {
                var indicator = CreateAndSetupIndicator();
                if (indicator is not null)
                {
                    availableIndicators.Enqueue(indicator);
                    indicatorsCreated++;
                }
            }
        }
        GD.Print($"- Indicator Pool: {indicatorsCreated}/{targetIndicatorPoolSize}");

        InitializeSinglePool(damageParticleScene, availableParticles, targetParticlePoolSize, "Damage Particles");
        InitializeSinglePool(deathParticleScene, availableParticles, targetParticlePoolSize, "Death Particles");

        InitializeSinglePool(defaultProjectileScene, availableProjectiles, targetProjectilePoolSize, "Default Projectiles");

        gameplayPoolsInitialized = true;
        initializationStarted = false;
        GD.Print("GlobalAudioPlayer: Gameplay pools initialization complete.");
    }

    private void InitializeSinglePool<T>(PackedScene scene, Dictionary<PackedScene, Queue<T>> poolDict, int targetSize, string poolName) where T : Node
    {
        if (scene is null)
        {
            GD.PrintErr($"Cannot initialize pool '{poolName}': Scene is null.");
            return;
        }

        if (!poolDict.TryGetValue(scene, out var queue))
        {
            queue = new Queue<T>(targetSize);
            poolDict.Add(scene, queue);
        }
        else
        {
            GD.Print($"Pool '{poolName}' for scene {scene.ResourcePath} already existed. Adding items.");
        }

        int createdCount = 0;
        for (int i = 0; i < targetSize; i++)
        {
            T instance = null;
            if (typeof(T) == typeof(PooledParticleEffect)) instance = CreateAndSetupParticle(scene) as T;
            else if (typeof(T) == typeof(Projectile)) instance = CreateAndSetupProjectile(scene) as T;
            // Add other types if necessary

            if (instance is not null)
            {
                queue.Enqueue(instance);
                createdCount++;
            }
        }
        GD.Print($"- {poolName} Pool ({scene.ResourcePath}): {queue.Count}/{targetSize} (Added {createdCount})");
    }


    private AudioStreamPlayer CreateAndSetupPlayer1D()
    {
        AudioStreamPlayer audioPlayer = new() { Bus = SfxBusName };
        AddChild(audioPlayer);
        audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
        return audioPlayer;
    }

    private AudioStreamPlayer2D CreateAndSetupPlayer2D()
    {
        AudioStreamPlayer2D audioPlayer = new() { Bus = SfxBusName };
        AddChild(audioPlayer);
        audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
        return audioPlayer;
    }

    private PooledParticleEffect CreateAndSetupParticle(PackedScene scene)
    {
        if (scene is null) return null;
        var particle = scene.Instantiate<PooledParticleEffect>();
        if (particle is null)
        {
            GD.PrintErr($"Failed instantiate particle: {scene.ResourcePath}");
            return null;
        }
        particle.SourceScene = scene;
        particle.TopLevel = true;
        particle.Visible = false;
        particle.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(particle);
        return particle;
    }

    private DamageIndicator CreateAndSetupIndicator()
    {
        if (damageIndicatorScene is null)
        {
            GD.PrintErr("Indicator scene null.");
            return null;
        }
        var indicator = damageIndicatorScene.Instantiate<DamageIndicator>();
        if (indicator is null)
        {
            GD.PrintErr($"Failed instantiate indicator: {damageIndicatorScene.ResourcePath}");
            return null;
        }
        indicator.SourceScene = damageIndicatorScene;
        indicator.TopLevel = true;
        indicator.Visible = false;
        indicator.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(indicator);
        return indicator;
    }

    private Projectile CreateAndSetupProjectile(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("Cannot create projectile: scene is null.");
            return null;
        }
        var projectile = scene.Instantiate<Projectile>();
        if (projectile is null)
        {
            GD.PrintErr($"Failed to instantiate projectile from scene: {scene.ResourcePath}");
            return null;
        }
        projectile.SourceScene = scene;
        projectile.TopLevel = true;
        projectile.Visible = false;
        projectile.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(projectile);
        return projectile;
    }

    public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
    {
        if (stream is null) return;
        AudioStreamPlayer2D audioPlayer;
        if (availablePlayers2D.Count > 0)
        {
            audioPlayer = availablePlayers2D.Dequeue();
        }
        else
        {
            GD.Print("AudioPlayer2D pool empty! Creating new.");
            audioPlayer = CreateAndSetupPlayer2D();
        }
        audioPlayer.GlobalPosition = position;
        audioPlayer.Stream = stream;
        audioPlayer.VolumeDb = volumeDb;
        audioPlayer.Play();
    }

    public void PlaySound(AudioStream stream, float volumeDb = 0f)
    {
        if (stream is null) return;
        AudioStreamPlayer audioPlayer;
        if (availablePlayers1D.Count > 0)
        {
            audioPlayer = availablePlayers1D.Dequeue();
        }
        else
        {
            GD.Print("AudioPlayer1D pool empty! Creating new.");
            audioPlayer = CreateAndSetupPlayer1D();
        }
        audioPlayer.Stream = stream;
        audioPlayer.VolumeDb = volumeDb;
        audioPlayer.Play();
    }

    public PooledParticleEffect GetParticleEffect(PackedScene scene, Vector2 position, Color? color = null)
    {
        if (scene is null)
        {
            GD.PrintErr("GetParticleEffect: Null scene.");
            return null;
        }

        if (!gameplayPoolsInitialized)
        {
            GD.PushWarning("GetParticleEffect called before gameplay pools fully initialized!");
        }

        if (!availableParticles.TryGetValue(scene, out var queue))
        {
            GD.PrintErr($"GetParticleEffect: Pool not found for {scene.ResourcePath}! Should exist. Creating fallback instance.");
            var fallbackParticle = CreateAndSetupParticle(scene);
            if (fallbackParticle is null) return null;
            fallbackParticle.GlobalPosition = position;
            fallbackParticle.ZIndex = ParticleZIndex;
            if (color.HasValue) fallbackParticle.Color = color.Value;
            fallbackParticle.Visible = true;
            fallbackParticle.ProcessMode = ProcessModeEnum.Inherit;
            fallbackParticle.PlayEffect();
            return fallbackParticle;
        }

        PooledParticleEffect particle;
        if (queue.Count > 0)
        {
            particle = queue.Dequeue();
            if (particle is null || !IsInstanceValid(particle))
            {
                GD.PrintErr("Invalid particle in pool. Creating replacement.");
                particle = CreateAndSetupParticle(scene);
                if (particle is null) return null;
            }
        }
        else
        {
            GD.Print($"Particle pool empty for {scene.ResourcePath}! Creating new instance.");
            particle = CreateAndSetupParticle(scene);
            if (particle is null) return null;
        }

        particle.GlobalPosition = position;
        particle.ZIndex = ParticleZIndex;
        if (color.HasValue) particle.Color = color.Value;
        particle.Visible = true;
        particle.ProcessMode = ProcessModeEnum.Inherit;
        particle.PlayEffect();
        return particle;
    }

    public DamageIndicator GetDamageIndicator()
    {
        if (!gameplayPoolsInitialized)
        {
            GD.PushWarning("GetDamageIndicator called before gameplay pools fully initialized!");
        }

        DamageIndicator indicator;
        if (availableIndicators.Count > 0)
        {
            indicator = availableIndicators.Dequeue();
            if (indicator is null || !IsInstanceValid(indicator))
            {
                GD.PrintErr("Invalid indicator in pool. Creating replacement.");
                indicator = CreateAndSetupIndicator();
                if (indicator is null) return null;
            }
        }
        else
        {
            GD.Print("Indicator pool empty! Creating new instance.");
            indicator = CreateAndSetupIndicator();
            if (indicator is null) return null;
        }

        indicator.Visible = true;
        indicator.ProcessMode = ProcessModeEnum.Inherit;
        indicator.Modulate = Colors.White;
        indicator.AnimatedAlpha = 1.0f;
        indicator.Scale = Vector2.One;
        return indicator;
    }

    public Projectile GetProjectile(PackedScene scene)
    {
        if (scene is null)
        {
            GD.PrintErr("GetProjectile: Null scene provided.");
            return null;
        }

        if (!gameplayPoolsInitialized)
        {
            GD.PushWarning("GetProjectile called before gameplay pools fully initialized!");
        }

        if (!availableProjectiles.TryGetValue(scene, out var queue))
        {
            GD.PrintErr($"GetProjectile: Pool not found for {scene.ResourcePath}! Should exist. Creating fallback instance.");
            var fallbackProjectile = CreateAndSetupProjectile(scene);
            if (fallbackProjectile is null) return null;
            fallbackProjectile.Visible = true;
            fallbackProjectile.ProcessMode = ProcessModeEnum.Disabled; // Start disabled
                                                                       // Don't Activate() here, let the caller do it
            return fallbackProjectile;
        }

        Projectile projectile;
        if (queue.Count > 0)
        {
            projectile = queue.Dequeue();
            if (projectile is null || !IsInstanceValid(projectile))
            {
                GD.PrintErr($"Invalid projectile found in pool for {scene.ResourcePath}. Creating replacement.");
                projectile = CreateAndSetupProjectile(scene);
                if (projectile is null) return null;
            }
        }
        else
        {
            GD.Print($"Projectile pool empty for {scene.ResourcePath}! Creating new instance.");
            projectile = CreateAndSetupProjectile(scene);
            if (projectile is null) return null;
        }

        // Reset state before returning, caller will Activate
        // Resetting is done in ReturnProjectileToPool via ResetForPooling,
        // but ensure key properties are set correctly for a 'new' instance feel.
        projectile.Visible = false; // Should start invisible
        projectile.ProcessMode = ProcessModeEnum.Disabled; // Should start disabled
        return projectile;
    }

    private void ReturnPlayerToPool(AudioStreamPlayer audioPlayer)
    {
        if (audioPlayer is null || !IsInstanceValid(audioPlayer)) return;
        audioPlayer.Stream = null;
        availablePlayers1D.Enqueue(audioPlayer);
    }

    private void ReturnPlayerToPool(AudioStreamPlayer2D audioPlayer)
    {
        if (audioPlayer is null || !IsInstanceValid(audioPlayer)) return;
        audioPlayer.Stream = null;
        availablePlayers2D.Enqueue(audioPlayer);
    }

    public void ReturnParticleToPool(PooledParticleEffect particle, PackedScene sourceScene)
    {
        if (particle is null || !IsInstanceValid(particle) || sourceScene is null)
        {
            particle?.QueueFree();
            return;
        }
        if (!availableParticles.TryGetValue(sourceScene, out var queue))
        {
            GD.PrintErr($"Particle pool not found for {sourceScene.ResourcePath} on return. Freeing particle.");
            particle.QueueFree();
            return;
        }
        particle.Visible = false;
        particle.ProcessMode = ProcessModeEnum.Disabled;
        particle.Emitting = false;
        queue.Enqueue(particle);
    }

    public void ReturnIndicatorToPool(DamageIndicator indicator)
    {
        if (indicator is null || !IsInstanceValid(indicator)) return;
        indicator.Visible = false;
        indicator.ProcessMode = ProcessModeEnum.Disabled;
        indicator.ResetForPooling();
        availableIndicators.Enqueue(indicator);
    }

    public void ReturnProjectileToPool(Projectile projectile, PackedScene sourceScene)
    {
        if (projectile is null || !IsInstanceValid(projectile)) return;
        if (sourceScene is null)
        {
            GD.PrintErr($"Projectile {projectile.GetInstanceId()} cannot return to pool: SourceScene is null. Freeing.");
            projectile.QueueFree();
            return;
        }

        if (!availableProjectiles.TryGetValue(sourceScene, out var queue))
        {
            GD.PrintErr($"Projectile pool not found for {sourceScene.ResourcePath} on return. Freeing projectile {projectile.GetInstanceId()}.");
            projectile.QueueFree();
            return;
        }
        projectile.ResetForPooling(); // This now handles setting Visible=false, ProcessMode=Disabled etc.
        queue.Enqueue(projectile);
    }

    // NEW Method
    public void CleanUpActiveGameObjects()
    {
        GD.Print("GlobalAudioPlayer: Cleaning up active game objects before scene change...");
        var nodesToClean = new List<Node>();

        foreach (Node child in GetChildren())
        {
            // Check specifically for Projectiles that might be active (ProcessMode != Disabled)
            // We check ProcessMode because ResetForPooling sets it to Disabled.
            if (child is Projectile projectile && projectile.ProcessMode != ProcessModeEnum.Disabled)
            {
                nodesToClean.Add(projectile);
            }
            // Add checks for other types like DamageIndicator or PooledParticleEffect if they cause similar issues
        }

        GD.Print($"GlobalAudioPlayer: Found {nodesToClean.Count} potentially active nodes to clean.");

        foreach (Node node in nodesToClean)
        {
            if (node is Projectile projectile)
            {
                // Ensure it's still valid and has a scene reference to return to the correct pool
                if (IsInstanceValid(projectile) && projectile.SourceScene is not null)
                {
                    GD.Print($" - Cleaning active Projectile: {projectile.GetInstanceId()}");
                    // Directly return it to the pool. This calls ResetForPooling internally.
                    ReturnProjectileToPool(projectile, projectile.SourceScene);
                }
                else if (IsInstanceValid(projectile))
                {
                    // If it's invalid or has no source scene, just free it.
                    GD.Print($" - Freeing active Projectile without SourceScene or invalid state: {projectile.GetInstanceId()}");
                    projectile.QueueFree();
                }
            }
            // Handle other types if needed
        }
        GD.Print("GlobalAudioPlayer: Finished cleaning active game objects.");
    }


    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }
}