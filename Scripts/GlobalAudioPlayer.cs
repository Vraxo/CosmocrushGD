using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Added for Task

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
	private const int TargetAudioPoolSize = 10; // Keep audio pool modest, initialize early

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
	private bool _gameplayPoolsInitialized = false;


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

		// Initialize basic things needed immediately (like UI sound and audio players)
		LoadMinimalResources();
		InitializeMinimalPools();
	}

	private void LoadMinimalResources()
	{
		// Load only things needed potentially before main game (like UI sound)
		UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");
		if (UiSound is null) GD.PrintErr("Failed to load UiSound");
	}

	private void InitializeMinimalPools()
	{
		// Initialize only audio players synchronously, as they are lightweight
		GD.Print("GlobalAudioPlayer: Initializing minimal (audio) pools...");
		for (int i = 0; i < TargetAudioPoolSize; i++)
		{
			availablePlayers1D.Enqueue(CreateAndSetupPlayer1D());
			availablePlayers2D.Enqueue(CreateAndSetupPlayer2D());
		}
		GD.Print($"- Audio Pools: {availablePlayers1D.Count}/{TargetAudioPoolSize}");
	}

	// Method called by SceneTransitionManager during black screen
	public async Task InitializeGameplayPoolsAsync()
	{
		// Prevent re-initialization
		if (_gameplayPoolsInitialized)
		{
			return;
		}

		GD.Print("GlobalAudioPlayer: Initializing gameplay pools (Particles, Indicators, Projectiles)...");

		// --- Load Resources Needed for Gameplay Pools ---
		// Allow yielding briefly in case resource loading itself causes hitches
		await Task.Yield();
		damageParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageParticleEffect.tscn");
		await Task.Yield();
		deathParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DeathParticleEffect.tscn");
		await Task.Yield();
		damageIndicatorScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageIndicator.tscn");
		await Task.Yield();
		defaultProjectileScene = ResourceLoader.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");
		await Task.Yield();

		if (damageParticleScene is null) GD.PrintErr("Failed to load DamageParticleEffect.tscn");
		if (deathParticleScene is null) GD.PrintErr("Failed to load DeathParticleEffect.tscn");
		if (damageIndicatorScene is null) GD.PrintErr("Failed to load DamageIndicator.tscn");
		if (defaultProjectileScene is null) GD.PrintErr("Failed to load default Projectile.tscn");


		// --- Initialize Pools Synchronously (Now safe during black screen) ---

		// Indicators
		int indicatorsCreated = 0;
		if (damageIndicatorScene is not null)
		{
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

		// Particles
		InitializeSinglePool(damageParticleScene, availableParticles, targetParticlePoolSize, "Damage Particles");
		InitializeSinglePool(deathParticleScene, availableParticles, targetParticlePoolSize, "Death Particles");

		// Projectiles
		InitializeSinglePool(defaultProjectileScene, availableProjectiles, targetProjectilePoolSize, "Default Projectiles");

		_gameplayPoolsInitialized = true;
		GD.Print("GlobalAudioPlayer: Gameplay pools initialization complete.");
	}

	// Generic Pool Initializer Helper (Modified to check if pool exists)
	private void InitializeSinglePool<T>(PackedScene scene, Dictionary<PackedScene, Queue<T>> poolDict, int targetSize, string poolName) where T : Node
	{
		if (scene is null)
		{
			GD.PrintErr($"Cannot initialize pool '{poolName}': Scene is null.");
			return;
		}

		// Ensure the dictionary entry exists before adding to queue
		if (!poolDict.TryGetValue(scene, out var queue))
		{
			queue = new Queue<T>(targetSize);
			poolDict.Add(scene, queue);
		}
		else
		{
			// Pool might already exist if InitializeSinglePool is somehow called again,
			// or if Get... created it on demand before full initialization.
			// Clear it? Or just log? For now, log and continue adding.
			GD.Print($"Pool '{poolName}' for scene {scene.ResourcePath} already existed. Adding items.");
		}

		int createdCount = 0;
		for (int i = 0; i < targetSize; i++)
		{
			T instance = null;
			if (typeof(T) == typeof(PooledParticleEffect)) instance = CreateAndSetupParticle(scene) as T;
			else if (typeof(T) == typeof(Projectile)) instance = CreateAndSetupProjectile(scene) as T;
			// Add other types here if needed

			if (instance is not null)
			{
				queue.Enqueue(instance);
				createdCount++;
			}
		}
		GD.Print($"- {poolName} Pool ({scene.ResourcePath}): {queue.Count}/{targetSize} (Added {createdCount})");
	}


	// --- Create and Setup Methods (Unchanged) ---

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
		if (particle is null) { GD.PrintErr($"Failed instantiate particle: {scene.ResourcePath}"); return null; }
		particle.SourceScene = scene;
		particle.TopLevel = true;
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(particle);
		return particle;
	}

	private DamageIndicator CreateAndSetupIndicator()
	{
		if (damageIndicatorScene is null) { GD.PrintErr("Indicator scene null."); return null; }
		var indicator = damageIndicatorScene.Instantiate<DamageIndicator>();
		if (indicator is null) { GD.PrintErr($"Failed instantiate indicator: {damageIndicatorScene.ResourcePath}"); return null; }
		indicator.SourceScene = damageIndicatorScene;
		indicator.TopLevel = true;
		indicator.Visible = false;
		indicator.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(indicator);
		return indicator;
	}

	private Projectile CreateAndSetupProjectile(PackedScene scene)
	{
		if (scene is null) { GD.PrintErr("Cannot create projectile: scene is null."); return null; }
		var projectile = scene.Instantiate<Projectile>();
		if (projectile is null) { GD.PrintErr($"Failed to instantiate projectile from scene: {scene.ResourcePath}"); return null; }
		projectile.SourceScene = scene;
		projectile.TopLevel = true;
		projectile.Visible = false;
		projectile.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(projectile);
		return projectile;
	}

	// --- Get/Return Methods (Remain largely the same, assuming pools are initialized) ---

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
			audioPlayer = CreateAndSetupPlayer2D(); // Fallback
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
			audioPlayer = CreateAndSetupPlayer1D(); // Fallback
		}
		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.Play();
	}

	public PooledParticleEffect GetParticleEffect(PackedScene scene, Vector2 position, Color? color = null)
	{
		if (scene is null) { GD.PrintErr("GetParticleEffect: Null scene."); return null; }

		if (!_gameplayPoolsInitialized) GD.PushWarning("GetParticleEffect called before gameplay pools fully initialized!");

		if (!availableParticles.TryGetValue(scene, out var queue))
		{
			GD.PrintErr($"GetParticleEffect: Pool not found for {scene.ResourcePath}! Creating fallback instance.");
			var fallbackParticle = CreateAndSetupParticle(scene);
			if (fallbackParticle is null) return null;
			// Don't add to dictionary here, let initialization handle it.
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
				particle = CreateAndSetupParticle(scene); // Replace broken one
				if (particle is null) return null;
			}
		}
		else
		{
			GD.Print($"Particle pool empty for {scene.ResourcePath}! Creating new instance.");
			particle = CreateAndSetupParticle(scene); // Create if pool depleted
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
		if (damageIndicatorScene is null) { GD.PrintErr("Indicator scene null."); return null; } // Should be loaded by init

		if (!_gameplayPoolsInitialized) GD.PushWarning("GetDamageIndicator called before gameplay pools fully initialized!");

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
		if (scene is null) { GD.PrintErr("GetProjectile: Null scene provided."); return null; }

		if (!_gameplayPoolsInitialized) GD.PushWarning("GetProjectile called before gameplay pools fully initialized!");

		if (!availableProjectiles.TryGetValue(scene, out var queue))
		{
			GD.PrintErr($"GetProjectile: Pool not found for {scene.ResourcePath}! Creating fallback instance.");
			var fallbackProjectile = CreateAndSetupProjectile(scene);
			if (fallbackProjectile is null) return null;
			// Don't add to dictionary here, let initialization handle it.
			fallbackProjectile.Visible = true;
			fallbackProjectile.ProcessMode = ProcessModeEnum.Disabled;
			return fallbackProjectile;
		}

		Projectile projectile;
		if (queue.Count > 0)
		{
			projectile = queue.Dequeue();
			if (projectile is null || !IsInstanceValid(projectile))
			{
				GD.PrintErr($"Invalid projectile found in pool for {scene.ResourcePath}. Creating replacement.");
				projectile = CreateAndSetupProjectile(scene); // Replace broken one
				if (projectile is null) return null;
			}
		}
		else
		{
			GD.Print($"Projectile pool empty for {scene.ResourcePath}! Creating new instance.");
			projectile = CreateAndSetupProjectile(scene); // Create if pool depleted
			if (projectile is null) return null;
		}

		projectile.Visible = true;
		projectile.ProcessMode = ProcessModeEnum.Disabled;
		return projectile;
	}

	// --- Return Methods (Unchanged) ---

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
			particle?.QueueFree(); return;
		}
		if (!availableParticles.TryGetValue(sourceScene, out var queue))
		{
			GD.PrintErr($"Particle pool not found for {sourceScene.ResourcePath} on return. Freeing particle.");
			particle.QueueFree(); return;
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
		if (projectile is null || !IsInstanceValid(projectile) || sourceScene is null)
		{
			projectile?.QueueFree(); return;
		}
		if (!availableProjectiles.TryGetValue(sourceScene, out var queue))
		{
			GD.PrintErr($"Projectile pool not found for {sourceScene.ResourcePath} on return. Freeing projectile.");
			projectile.QueueFree(); return;
		}
		projectile.ResetForPooling();
		queue.Enqueue(projectile);
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
