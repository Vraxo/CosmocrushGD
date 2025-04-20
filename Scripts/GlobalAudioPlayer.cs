using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	public static GlobalAudioPlayer Instance { get; private set; }

	public AudioStream UiSound { get; private set; }

	private PackedScene damageParticleScene;
	private PackedScene deathParticleScene;
	private int initialParticlePoolSize = 20;

	private const string SfxBusName = "SFX";
	private const int InitialAudioPoolSize = 10;
	private const string DamageParticleScenePath = "res://Scenes/DamageParticleEffect.tscn";
	private const string DeathParticleScenePath = "res://Scenes/DeathParticleEffect.tscn";
	private const string UiSoundPath = "res://Audio/SFX/Ui.mp3";

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
	private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();
	private Node particleContainer;

	public override void _EnterTree()
	{
		if (Instance is not null)
		{
			QueueFree();
			return;
		}
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		LoadResources();
		CreateParticleContainer(); // Call the method that now uses CallDeferred
		InitializeAudioPools();
		InitializeParticlePools();
	}

	private void LoadResources()
	{
		UiSound = ResourceLoader.Load<AudioStream>(UiSoundPath);
		if (UiSound is null)
		{
			GD.PrintErr($"GlobalAudioPlayer: Failed to load {UiSoundPath}");
		}

		damageParticleScene = ResourceLoader.Load<PackedScene>(DamageParticleScenePath);
		if (damageParticleScene is null)
		{
			GD.PrintErr($"GlobalAudioPlayer: Failed to load {DamageParticleScenePath}");
		}

		deathParticleScene = ResourceLoader.Load<PackedScene>(DeathParticleScenePath);
		if (deathParticleScene is null)
		{
			GD.PrintErr($"GlobalAudioPlayer: Failed to load {DeathParticleScenePath}");
		}
	}

	private void CreateParticleContainer()
	{
		particleContainer = new Node2D
		{
			Name = "ParticleContainer"
		};
		// Use CallDeferred to add the child safely during _EnterTree
		GetTree().Root.CallDeferred(Node.MethodName.AddChild, particleContainer);
	}

	private void InitializeAudioPools()
	{
		for (int i = 0; i < InitialAudioPoolSize; i++)
		{
			availablePlayers1D.Enqueue(CreateAndSetupPlayer1D());
			availablePlayers2D.Enqueue(CreateAndSetupPlayer2D());
		}
	}

	private void InitializeParticlePools()
	{
		// Need to ensure particleContainer exists before initializing particles
		// Since AddChild is deferred, we should defer initialization too.
		CallDeferred(nameof(DeferredInitializeParticlePools));
	}

	private void DeferredInitializeParticlePools()
	{
		if (particleContainer is null || !IsInstanceValid(particleContainer))
		{
			GD.PrintErr("GlobalAudioPlayer: ParticleContainer is null or invalid during deferred initialization. Cannot initialize particle pools.");
			// This might happen if CreateParticleContainer failed severely, though unlikely with deferred call.
			return;
		}
		GD.Print("GlobalAudioPlayer: Starting deferred particle pool initialization.");
		InitializeSingleParticlePool(damageParticleScene);
		InitializeSingleParticlePool(deathParticleScene);
		GD.Print("GlobalAudioPlayer: Finished deferred particle pool initialization.");
	}


	private void InitializeSingleParticlePool(PackedScene particleScene)
	{
		if (particleScene is null)
		{
			GD.PrintErr($"InitializeSingleParticlePool: Attempted to initialize with a null scene resource.");
			return;
		}

		if (!IsInstanceValid(particleScene))
		{
			GD.PrintErr($"InitializeSingleParticlePool: Attempted to initialize with an invalid PackedScene instance for path: {particleScene.ResourcePath}");
			return;
		}

		// Ensure particleContainer is ready before creating/adding particles
		if (particleContainer is null || !IsInstanceValid(particleContainer))
		{
			GD.PrintErr($"InitializeSingleParticlePool: ParticleContainer is not ready for scene {particleScene.ResourcePath}. Aborting pool initialization for this scene.");
			return;
		}

		string scenePath = particleScene.ResourcePath ?? "Unknown Scene";
		GD.Print($"InitializeSingleParticlePool: Initializing pool for {scenePath}");

		if (!availableParticles.ContainsKey(particleScene))
		{
			availableParticles.Add(particleScene, new Queue<PooledParticleEffect>());
		}

		Queue<PooledParticleEffect> queue = availableParticles[particleScene];
		int initialCount = queue.Count;
		int createdCount = 0;

		for (int i = 0; i < initialParticlePoolSize; i++)
		{
			var particle = CreateAndSetupParticle(particleScene);
			if (particle is not null)
			{
				queue.Enqueue(particle);
				createdCount++;
			}
			else
			{
				GD.PrintErr($"InitializeSingleParticlePool: Failed to create particle instance {i + 1} for {scenePath}");
			}
		}
		GD.Print($"Initialized particle pool for {scenePath}. Added {createdCount} instances. Total in pool: {queue.Count}. Target size: {initialParticlePoolSize}");
	}

	private AudioStreamPlayer CreateAndSetupPlayer1D()
	{
		AudioStreamPlayer audioPlayer = new()
		{
			Bus = SfxBusName,
			ProcessMode = ProcessModeEnum.Always // Ensure player works when paused if needed
		};
		AddChild(audioPlayer);
		audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
		return audioPlayer;
	}

	private AudioStreamPlayer2D CreateAndSetupPlayer2D()
	{
		AudioStreamPlayer2D audioPlayer = new()
		{
			Bus = SfxBusName,
			ProcessMode = ProcessModeEnum.Always // Ensure player works when paused if needed
		};
		AddChild(audioPlayer);
		audioPlayer.Finished += () => ReturnPlayerToPool(audioPlayer);
		return audioPlayer;
	}

	private PooledParticleEffect CreateAndSetupParticle(PackedScene scene)
	{
		if (scene is null)
		{
			GD.PrintErr("CreateAndSetupParticle: Provided scene was null.");
			return null;
		}
		if (!IsInstanceValid(scene))
		{
			GD.PrintErr($"CreateAndSetupParticle: Provided scene resource is invalid: {scene?.ResourcePath ?? "Path unknown"}");
			return null;
		}

		Node instance = scene.Instantiate();
		if (instance is not PooledParticleEffect particle)
		{
			GD.PrintErr($"CreateAndSetupParticle: Failed to instantiate scene {scene.ResourcePath} as PooledParticleEffect.");
			instance?.QueueFree(); // Clean up if it instantiated as something else
			return null;
		}

		particle.SourceScene = scene;
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		particle.Emitting = false;

		// Add to the GlobalAudioPlayer itself initially, it will be reparented when activated.
		AddChild(particle);
		return particle;
	}

	public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
	{
		if (stream is null)
		{
			GD.PrintErr("PlaySound2D: Attempted to play a null AudioStream.");
			return;
		}

		AudioStreamPlayer2D audioPlayer;
		if (availablePlayers2D.Count > 0)
		{
			audioPlayer = availablePlayers2D.Dequeue();
			if (audioPlayer is null || !IsInstanceValid(audioPlayer))
			{
				GD.Print("GlobalAudioPlayer: Dequeued invalid 2D player, creating new.");
				audioPlayer = CreateAndSetupPlayer2D();
			}
		}
		else
		{
			GD.Print("GlobalAudioPlayer: 2D audio pool empty, creating new player.");
			audioPlayer = CreateAndSetupPlayer2D();
		}

		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.GlobalPosition = position;
		audioPlayer.Play();
	}

	public void PlaySound(AudioStream stream, float volumeDb = 0f)
	{
		if (stream is null)
		{
			GD.PrintErr("PlaySound: Attempted to play a null AudioStream.");
			return;
		}

		AudioStreamPlayer audioPlayer;
		if (availablePlayers1D.Count > 0)
		{
			audioPlayer = availablePlayers1D.Dequeue();
			if (audioPlayer is null || !IsInstanceValid(audioPlayer))
			{
				GD.Print("GlobalAudioPlayer: Dequeued invalid 1D player, creating new.");
				audioPlayer = CreateAndSetupPlayer1D();
			}
		}
		else
		{
			GD.Print("GlobalAudioPlayer: 1D audio pool empty, creating new player.");
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
			GD.PrintErr("GetParticleEffect: Attempted to get particle with null scene resource.");
			return null;
		}
		if (!IsInstanceValid(scene))
		{
			GD.PrintErr($"GetParticleEffect: Attempted to get particle with an invalid PackedScene instance (Path: {scene.ResourcePath}).");
			return null;
		}
		if (particleContainer is null || !IsInstanceValid(particleContainer))
		{
			GD.PrintErr("GetParticleEffect: ParticleContainer node is invalid or null. Cannot place particle.");
			return null;
		}

		string scenePath = scene.ResourcePath ?? "Unknown Scene";

		if (!availableParticles.TryGetValue(scene, out Queue<PooledParticleEffect> queue))
		{
			GD.PrintErr($"GetParticleEffect: Pool not found for scene {scenePath}. Was it loaded/initialized correctly? Attempting to create a new one as fallback.");
			return CreateAndActivateFallbackParticle(scene, position, color);
		}

		PooledParticleEffect particle = null;
		while (queue.Count > 0 && (particle is null || !IsInstanceValid(particle)))
		{
			if (particle is not null) // It was invalid
			{
				GD.Print($"GetParticleEffect: Found invalid particle in pool for {scenePath}. Discarding.");
				// Don't QueueFree here, let GC handle it or it might interfere if return is pending
			}
			particle = queue.Dequeue();
		}


		if (particle is null || !IsInstanceValid(particle))
		{
			GD.Print($"GetParticleEffect: Particle pool empty or contained only invalid instances for {scenePath}. Creating new instance as fallback.");
			return CreateAndActivateFallbackParticle(scene, position, color);
		}


		ReparentParticle(particle, particleContainer);

		particle.GlobalPosition = position;
		if (color.HasValue)
		{
			particle.Color = color.Value;
		}
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Inherit;
		particle.PlayEffect();

		return particle;
	}

	private PooledParticleEffect CreateAndActivateFallbackParticle(PackedScene scene, Vector2 position, Color? color)
	{
		GD.Print($"CreateAndActivateFallbackParticle: Creating fallback particle for {scene.ResourcePath ?? "Unknown Scene"}");
		var particle = CreateAndSetupParticle(scene);
		if (particle is null)
		{
			GD.PrintErr($"CreateAndActivateFallbackParticle: Failed to create fallback instance for {scene.ResourcePath ?? "Unknown Scene"}.");
			return null; // Creation failed
		}

		if (particleContainer is null || !IsInstanceValid(particleContainer))
		{
			GD.PrintErr("CreateAndActivateFallbackParticle: ParticleContainer is null. Cannot add fallback particle to scene tree.");
			particle.QueueFree(); // Clean up the newly created particle
			return null;
		}

		ReparentParticle(particle, particleContainer);

		particle.GlobalPosition = position;
		if (color.HasValue)
		{
			particle.Color = color.Value;
		}
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Inherit;
		particle.PlayEffect(); // Start emitting and the return timer

		GD.Print($"CreateAndActivateFallbackParticle: Fallback particle created and activated for {scene.ResourcePath ?? "Unknown Scene"}. This might indicate pool depletion or initialization issues.");
		return particle; // Return the fallback instance
	}

	private void ReparentParticle(Node particle, Node newParent)
	{
		if (particle is null || !IsInstanceValid(particle) || newParent is null || !IsInstanceValid(newParent))
		{
			GD.PrintErr("ReparentParticle: Invalid node provided for reparenting.");
			return;
		}

		Node currentParent = particle.GetParent();
		if (currentParent != newParent)
		{
			// Need to defer RemoveChild/AddChild if the tree might be locked
			// Using CallDeferred for both ensures the sequence happens correctly in the next idle frame
			currentParent?.CallDeferred(Node.MethodName.RemoveChild, particle);
			newParent.CallDeferred(Node.MethodName.AddChild, particle);
		}
	}

	private void ReturnPlayerToPool(AudioStreamPlayer audioPlayer)
	{
		if (audioPlayer is null || !IsInstanceValid(audioPlayer))
		{
			GD.Print("ReturnPlayerToPool (1D): Attempted to return invalid player.");
			return;
		}
		audioPlayer.Stream = null;
		availablePlayers1D.Enqueue(audioPlayer);
	}

	private void ReturnPlayerToPool(AudioStreamPlayer2D audioPlayer)
	{
		if (audioPlayer is null || !IsInstanceValid(audioPlayer))
		{
			GD.Print("ReturnPlayerToPool (2D): Attempted to return invalid player.");
			return;
		}
		audioPlayer.Stream = null;
		audioPlayer.GlobalPosition = Vector2.Zero;
		availablePlayers2D.Enqueue(audioPlayer);
	}

	public void ReturnParticleToPool(PooledParticleEffect particle)
	{
		if (particle is null || !IsInstanceValid(particle))
		{
			GD.PrintErr("ReturnParticleToPool: Attempted to return a null or invalid particle instance.");
			return;
		}

		PackedScene sourceScene = particle.SourceScene;
		if (sourceScene is null || !IsInstanceValid(sourceScene))
		{
			GD.PrintErr($"ReturnParticleToPool: Particle '{particle.Name}' has a null or invalid SourceScene. Cannot return to pool. Freeing.");
			particle.QueueFree();
			return;
		}

		string scenePath = sourceScene.ResourcePath ?? "Unknown Scene";

		if (!availableParticles.TryGetValue(sourceScene, out Queue<PooledParticleEffect> queue))
		{
			GD.PrintErr($"ReturnParticleToPool: Pool not found for scene {scenePath}. Freeing particle '{particle.Name}'.");
			particle.QueueFree();
			return;
		}

		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		particle.Emitting = false;
		particle.GlobalPosition = Vector2.Zero;

		// Also defer reparenting when returning to the pool
		ReparentParticle(particle, this);

		queue.Enqueue(particle);
	}

	public override void _ExitTree()
	{
		if (particleContainer is not null && IsInstanceValid(particleContainer))
		{
			particleContainer.QueueFree();
			particleContainer = null;
		}

		// Clean up remaining pooled objects
		foreach (var player in availablePlayers1D) { player?.QueueFree(); }
		foreach (var player in availablePlayers2D) { player?.QueueFree(); }
		foreach (var queue in availableParticles.Values)
		{
			foreach (var particle in queue) { particle?.QueueFree(); }
		}
		availablePlayers1D.Clear();
		availablePlayers2D.Clear();
		availableParticles.Clear();


		if (Instance == this)
		{
			Instance = null;
		}
		base._ExitTree();
	}
}
