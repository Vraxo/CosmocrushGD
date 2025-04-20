using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	private static GlobalAudioPlayer _instance;
	public static GlobalAudioPlayer Instance => _instance;

	public AudioStream UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");

	// Remove [Export] attributes
	private PackedScene damageParticleScene;
	private PackedScene deathParticleScene;
	// Keep export for pool size if you want to configure it via scene (if using Solution 1)
	// Or set it directly here if using Solution 2
	private int initialParticlePoolSize = 20;

	private const string SfxBusName = "SFX";
	private const int InitialAudioPoolSize = 10;

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
	private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();


	public override void _EnterTree()
	{
		if (_instance is not null)
		{
			QueueFree();
			return;
		}
		_instance = this;
		ProcessMode = ProcessModeEnum.Always;

		// Load particle scenes here if not using exports
		damageParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageParticleEffect.tscn");
		deathParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DeathParticleEffect.tscn");
		if (damageParticleScene is null) GD.PrintErr("Failed to load DamageParticleEffect.tscn");
		if (deathParticleScene is null) GD.PrintErr("Failed to load DeathParticleEffect.tscn");


		InitializeAudioPools();
		InitializeParticlePools(); // Initialize particle pools
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
		// Initialization logic remains the same, using the loaded scenes
		if (damageParticleScene is not null)
		{
			InitializeSingleParticlePool(damageParticleScene);
		}
		else
		{
			GD.PrintErr("GlobalAudioPlayer: Damage Particle Scene could not be loaded!");
		}

		if (deathParticleScene is not null)
		{
			InitializeSingleParticlePool(deathParticleScene);
		}
		else
		{
			GD.PrintErr("GlobalAudioPlayer: Death Particle Scene could not be loaded!");
		}
	}

	// ... (rest of the script remains the same as the previous version with debug prints)


	private void InitializeSingleParticlePool(PackedScene particleScene)
	{
		if (particleScene is null)
		{
			GD.PrintErr("InitializeSingleParticlePool: Attempted to initialize with a null scene.");
			return;
		}

		GD.Print($"InitializeSingleParticlePool: Attempting to initialize pool for {particleScene.ResourcePath}"); // Debug Print 1
		if (!availableParticles.ContainsKey(particleScene))
		{
			GD.Print($"InitializeSingleParticlePool: Creating new queue for {particleScene.ResourcePath}"); // Debug Print 2
			availableParticles.Add(particleScene, new Queue<PooledParticleEffect>());
		}
		else
		{
			GD.Print($"InitializeSingleParticlePool: Queue already exists for {particleScene.ResourcePath}"); // Debug Print 3
		}

		Queue<PooledParticleEffect> queue = availableParticles[particleScene];
		int initialCount = queue.Count;
		for (int i = 0; i < initialParticlePoolSize; i++)
		{
			var particle = CreateAndSetupParticle(particleScene);
			if (particle is not null)
			{
				queue.Enqueue(particle);
			}
		}
		GD.Print($"Initialized particle pool for {particleScene.ResourcePath}. Added {queue.Count - initialCount} instances. Total: {queue.Count}."); // Debug Print 4
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
		if (scene is null)
		{
			return null;
		}
		PooledParticleEffect particle = scene.Instantiate<PooledParticleEffect>();
		particle.SourceScene = scene; // Store the source scene
		particle.Visible = false; // Start invisible
		particle.ProcessMode = ProcessModeEnum.Disabled; // Start disabled
		AddChild(particle); // Add to the GlobalAudioPlayer node initially
		return particle;
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
			return;
		}

		AudioStreamPlayer audioPlayer;
		if (availablePlayers1D.Count > 0)
		{
			audioPlayer = availablePlayers1D.Dequeue();
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
			GD.PrintErr("GetParticleEffect: Attempted to get particle with null scene.");
			return null;
		}

		GD.Print($"GetParticleEffect: Attempting to get {scene.ResourcePath}. Available keys: [{string.Join(", ", availableParticles.Keys.Select(k => k?.ResourcePath ?? "NULL"))}]"); // Debug Print 5

		if (!availableParticles.TryGetValue(scene, out Queue<PooledParticleEffect> queue))
		{
			GD.PrintErr($"GetParticleEffect: Pool not found for scene {scene.ResourcePath}. Was it loaded/initialized in GlobalAudioPlayer?");
			// Fallback: Create a new one, but don't add to pool management here
			var newParticle = CreateAndSetupParticle(scene);
			if (newParticle is not null)
			{
				GetTree().Root.AddChild(newParticle); // Add to root so it's not child of player
				newParticle.GlobalPosition = position;
				newParticle.ProcessMode = ProcessModeEnum.Inherit;
				newParticle.Visible = true;
				if (color.HasValue) { newParticle.Color = color.Value; }
				newParticle.PlayEffect();
				return newParticle; // Return the fallback instance
			}
			return null; // Creation failed
		}

		PooledParticleEffect particle;
		if (queue.Count > 0)
		{
			particle = queue.Dequeue();
			if (particle is null || !IsInstanceValid(particle))
			{
				GD.PrintErr($"GetParticleEffect: Found invalid particle in pool for {scene.ResourcePath}. Creating new.");
				particle = CreateAndSetupParticle(scene);
				if (particle is null) return null; // Failed to create fallback
			}
		}
		else
		{
			GD.Print($"GetParticleEffect: Particle pool empty for {scene.ResourcePath}. Creating new instance.");
			particle = CreateAndSetupParticle(scene);
			if (particle is null) return null; // Failed to create fallback
		}

		// Reparent to the root node (or a dedicated particle container if you prefer)
		// This prevents particles from moving with the enemy/player
		if (particle.GetParent() != GetTree().Root)
		{
			particle.GetParent()?.RemoveChild(particle);
			GetTree().Root.AddChild(particle);
		}


		particle.GlobalPosition = position;
		if (color.HasValue) { particle.Color = color.Value; }
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Inherit; // Enable processing
		particle.PlayEffect(); // Start emitting and the return timer

		return particle;
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
		audioPlayer.GlobalPosition = Vector2.Zero;
		availablePlayers2D.Enqueue(audioPlayer);
	}

	public void ReturnParticleToPool(PooledParticleEffect particle, PackedScene sourceScene)
	{
		if (particle is null || !IsInstanceValid(particle) || sourceScene is null)
		{
			GD.PrintErr("ReturnParticleToPool: Invalid particle or sourceScene.");
			particle?.QueueFree(); // Clean up invalid particle
			return;
		}

		GD.Print($"ReturnParticleToPool: Attempting to return for {sourceScene.ResourcePath}. Available keys: [{string.Join(", ", availableParticles.Keys.Select(k => k?.ResourcePath ?? "NULL"))}]"); // Debug Print 6

		if (!availableParticles.TryGetValue(sourceScene, out Queue<PooledParticleEffect> queue))
		{
			GD.PrintErr($"ReturnParticleToPool: Pool not found for scene {sourceScene.ResourcePath}. Freeing particle.");
			particle.QueueFree();
			return;
		}

		// Reset state
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		particle.Emitting = false; // Ensure it stops emitting if somehow still active
		particle.GlobalPosition = Vector2.Zero; // Reset position

		// Reparent back to the GlobalAudioPlayer node
		if (particle.GetParent() != this)
		{
			particle.GetParent()?.RemoveChild(particle);
			AddChild(particle);
		}


		queue.Enqueue(particle);
	}
}
