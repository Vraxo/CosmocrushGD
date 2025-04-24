using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	public static GlobalAudioPlayer Instance { get; private set; }
	public AudioStream UiSound { get; private set; }

	private PackedScene damageParticleScene;
	private PackedScene deathParticleScene;
	private PackedScene damageIndicatorScene;
	private PackedScene defaultProjectileScene;

	private int targetParticlePoolSize = 60;
	private int targetIndicatorPoolSize = 90;
	private int targetProjectilePoolSize = 60;
	private const int TargetAudioPoolSize = 10;

	private const string SfxBusName = "SFX";
	private const int ParticleZIndex = 10;
	private const int IndicatorZIndex = 100; // Define a Z-index for indicators

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
	private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();
	private Queue<DamageIndicator> availableIndicators = new();
	private Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();

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

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		base._ExitTree();
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
			if (!availableIndicators.Any()) // Check if queue exists and needs filling
			{
				availableIndicators = new Queue<DamageIndicator>(targetIndicatorPoolSize);
			}

			// Fill only up to the target size
			int needed = targetIndicatorPoolSize - availableIndicators.Count;
			for (int i = 0; i < needed; i++)
			{
				var indicator = CreateAndSetupIndicator();
				if (indicator is not null)
				{
					availableIndicators.Enqueue(indicator);
					indicatorsCreated++;
				}
			}
		}
		GD.Print($"- Indicator Pool: {availableIndicators.Count}/{targetIndicatorPoolSize} (Added {indicatorsCreated})");

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
			GD.Print($"Pool '{poolName}' for scene {scene.ResourcePath} already existed. Ensuring target size.");
		}

		int needed = targetSize - queue.Count;
		int createdCount = 0;
		for (int i = 0; i < needed; i++)
		{
			T instance = null;
			if (typeof(T) == typeof(PooledParticleEffect)) instance = CreateAndSetupParticle(scene) as T;
			else if (typeof(T) == typeof(Projectile)) instance = CreateAndSetupProjectile(scene) as T;
			else if (typeof(T) == typeof(DamageIndicator)) instance = CreateAndSetupIndicator() as T;


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
		AudioStreamPlayer audioPlayer = new()
		{
			Bus = SfxBusName
		};
		AddChild(audioPlayer);
		audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
		return audioPlayer;
	}

	private AudioStreamPlayer2D CreateAndSetupPlayer2D()
	{
		AudioStreamPlayer2D audioPlayer = new()
		{
			Bus = SfxBusName
		};
		AddChild(audioPlayer);
		audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
		return audioPlayer;
	}

	private PooledParticleEffect CreateAndSetupParticle(PackedScene scene)
	{
		if (scene is null)
		{
			return null;
		}

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
		particle.ZIndex = ParticleZIndex; // Particles also need ZIndex if TopLevel
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
		indicator.ZIndex = IndicatorZIndex; // Set Z-Index during creation
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
		projectile.ZIndex = Projectile.ProjectileZIndex; // Projectiles also need ZIndex
		AddChild(projectile);
		return projectile;
	}

	public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
	{
		if (stream is null)
		{
			return;
		}

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
		if (stream is null)
		{
			return;
		}

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
			// Optionally, force initialization here or return null/fallback
		}

		if (!availableParticles.TryGetValue(scene, out var queue))
		{
			GD.PrintErr($"GetParticleEffect: Pool not found for {scene.ResourcePath}! Should exist. Creating fallback instance.");
			var fallbackParticle = CreateAndSetupParticle(scene);
			if (fallbackParticle is null)
			{
				return null;
			}

			fallbackParticle.GlobalPosition = position;
			// fallbackParticle.ZIndex = ParticleZIndex; // ZIndex set in CreateAndSetup
			if (color.HasValue)
			{
				fallbackParticle.Color = color.Value;
			}
			fallbackParticle.Visible = true;
			fallbackParticle.ProcessMode = ProcessModeEnum.Pausable; // Use Pausable for consistency
			fallbackParticle.PlayEffect();
			return fallbackParticle;
		}

		PooledParticleEffect particle;
		if (queue.Count > 0)
		{
			particle = queue.Dequeue();
			if (particle is null || !IsInstanceValid(particle))
			{
				GD.PrintErr($"Invalid particle in pool {scene.ResourcePath}. Creating replacement.");
				particle = CreateAndSetupParticle(scene);
				if (particle is null)
				{
					return null;
				}
			}
		}
		else
		{
			GD.Print($"Particle pool empty for {scene.ResourcePath}! Creating new instance.");
			particle = CreateAndSetupParticle(scene);
			if (particle is null)
			{
				return null;
			}
		}

		particle.GlobalPosition = position;
		// particle.ZIndex = ParticleZIndex; // ZIndex set in CreateAndSetup
		if (color.HasValue)
		{
			particle.Color = color.Value;
		}
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Pausable; // Use Pausable
		particle.PlayEffect();
		return particle;
	}

	public DamageIndicator GetDamageIndicator()
	{
		if (!gameplayPoolsInitialized)
		{
			GD.PushWarning("GetDamageIndicator called before gameplay pools fully initialized!");
			// Consider returning null or a fallback if pools are essential and not ready
		}

		DamageIndicator indicator;
		if (availableIndicators.Count > 0)
		{
			indicator = availableIndicators.Dequeue();
			if (indicator is null || !IsInstanceValid(indicator))
			{
				GD.PrintErr("Invalid indicator in pool. Creating replacement.");
				indicator = CreateAndSetupIndicator();
				if (indicator is null)
				{
					return null;
				}
			}
		}
		else
		{
			GD.Print("Indicator pool empty! Creating new instance.");
			indicator = CreateAndSetupIndicator();
			if (indicator is null)
			{
				return null;
			}
		}

		indicator.Visible = true;
		indicator.ProcessMode = ProcessModeEnum.Pausable; // Set ProcessMode to Pausable
		indicator.Modulate = Colors.White;
		indicator.AnimatedAlpha = 1.0f;
		indicator.Scale = Vector2.One;
		// indicator.ZIndex = IndicatorZIndex; // Z-Index is now set in CreateAndSetupIndicator

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
			if (fallbackProjectile is null)
			{
				return null;
			}
			// Fallback needs manual setup similar to pool retrieval
			fallbackProjectile.Visible = false;
			fallbackProjectile.ProcessMode = ProcessModeEnum.Disabled;
			// fallbackProjectile.ZIndex = Projectile.ProjectileZIndex; // Set in CreateAndSetup
			return fallbackProjectile; // Caller will Activate
		}

		Projectile projectile;
		if (queue.Count > 0)
		{
			projectile = queue.Dequeue();
			if (projectile is null || !IsInstanceValid(projectile))
			{
				GD.PrintErr($"Invalid projectile found in pool for {scene.ResourcePath}. Creating replacement.");
				projectile = CreateAndSetupProjectile(scene);
				if (projectile is null)
				{
					return null;
				}
			}
		}
		else
		{
			GD.Print($"Projectile pool empty for {scene.ResourcePath}! Creating new instance.");
			projectile = CreateAndSetupProjectile(scene);
			if (projectile is null)
			{
				return null;
			}
		}

		projectile.Visible = false;
		projectile.ProcessMode = ProcessModeEnum.Disabled;
		// projectile.ZIndex = Projectile.ProjectileZIndex; // Set in CreateAndSetup
		return projectile;
	}

	private void ReturnPlayerToPool(AudioStreamPlayer audioPlayer)
	{
		if (audioPlayer is null || !IsInstanceValid(audioPlayer))
		{
			return;
		}
		audioPlayer.Stream = null;
		availablePlayers1D.Enqueue(audioPlayer);
	}

	private void ReturnPlayerToPool(AudioStreamPlayer2D audioPlayer)
	{
		if (audioPlayer is null || !IsInstanceValid(audioPlayer))
		{
			return;
		}
		audioPlayer.Stream = null;
		availablePlayers2D.Enqueue(audioPlayer);
	}

	public void ReturnParticleToPool(PooledParticleEffect particle, PackedScene sourceScene)
	{
		if (particle is null || !IsInstanceValid(particle) || sourceScene is null)
		{
			GD.PrintErr($"ReturnParticleToPool: Invalid input. Particle: {particle?.GetInstanceId()}, SourceScene: {sourceScene?.ResourcePath}");
			particle?.QueueFree(); // Free if invalid or scene missing
			return;
		}
		if (!availableParticles.TryGetValue(sourceScene, out var queue))
		{
			GD.PrintErr($"Particle pool not found for {sourceScene.ResourcePath} on return. Freeing particle {particle.GetInstanceId()}.");
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
		if (indicator is null || !IsInstanceValid(indicator))
		{
			GD.PrintErr($"ReturnIndicatorToPool: Invalid indicator instance {indicator?.GetInstanceId()}.");
			return;
		}
		indicator.Visible = false;
		indicator.ProcessMode = ProcessModeEnum.Disabled;
		indicator.ResetForPooling();
		availableIndicators.Enqueue(indicator);
	}

	public void ReturnProjectileToPool(Projectile projectile, PackedScene sourceScene)
	{
		if (projectile is null || !IsInstanceValid(projectile))
		{
			GD.PrintErr($"ReturnProjectileToPool: Invalid projectile instance {projectile?.GetInstanceId()}.");
			return;
		}
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
		projectile.ResetForPooling();
		queue.Enqueue(projectile);
	}

	public void CleanUpActiveGameObjects()
	{
		GD.Print("GlobalAudioPlayer: Cleaning up active game objects before scene change...");
		var nodesToClean = new List<Node>();

		foreach (Node child in GetChildren())
		{
			// Add DamageIndicator and PooledParticleEffect to checks
			if (child is Projectile projectile && projectile.ProcessMode != ProcessModeEnum.Disabled)
			{
				nodesToClean.Add(projectile);
			}
			else if (child is DamageIndicator indicator && indicator.ProcessMode != ProcessModeEnum.Disabled)
			{
				nodesToClean.Add(indicator);
			}
			else if (child is PooledParticleEffect particle && particle.ProcessMode != ProcessModeEnum.Disabled)
			{
				nodesToClean.Add(particle);
			}
		}

		GD.Print($"GlobalAudioPlayer: Found {nodesToClean.Count} potentially active nodes to clean.");

		foreach (Node node in nodesToClean)
		{
			if (!IsInstanceValid(node)) // Check if node became invalid during iteration
			{
				continue;
			}

			if (node is Projectile projectile)
			{
				if (projectile.SourceScene is not null)
				{
					GD.Print($" - Cleaning active Projectile: {projectile.GetInstanceId()}");
					ReturnProjectileToPool(projectile, projectile.SourceScene);
				}
				else
				{
					GD.Print($" - Freeing active Projectile without SourceScene: {projectile.GetInstanceId()}");
					projectile.QueueFree();
				}
			}
			else if (node is DamageIndicator indicator)
			{
				// DamageIndicator doesn't strictly NEED a SourceScene to return, pool is singular
				GD.Print($" - Cleaning active DamageIndicator: {indicator.GetInstanceId()}");
				ReturnIndicatorToPool(indicator);
			}
			else if (node is PooledParticleEffect particle)
			{
				if (particle.SourceScene is not null)
				{
					GD.Print($" - Cleaning active PooledParticleEffect: {particle.GetInstanceId()}");
					ReturnParticleToPool(particle, particle.SourceScene);
				}
				else
				{
					GD.Print($" - Freeing active PooledParticleEffect without SourceScene: {particle.GetInstanceId()}");
					particle.QueueFree();
				}
			}
		}
		GD.Print("GlobalAudioPlayer: Finished cleaning active game objects.");
	}
}
