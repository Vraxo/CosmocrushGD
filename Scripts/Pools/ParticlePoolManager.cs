using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class ParticlePoolManager : Node
{
	public static ParticlePoolManager Instance { get; private set; }

	private PackedScene damageParticleScene;
	private PackedScene deathParticleScene;

	private int targetParticlePoolSize = 60;
	private const int ParticleZIndex = 10;

	private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();
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
			GD.Print($"ParticlePoolManager: Initialization skipped (Initialized: {poolsInitialized}, Started: {initializationStarted})");
			return;
		}
		initializationStarted = true;
		GD.Print("ParticlePoolManager: Starting initialization...");

		damageParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageParticleEffect.tscn");
		deathParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DeathParticleEffect.tscn");

		if (damageParticleScene is null) GD.PrintErr("ParticlePoolManager: Failed to load DamageParticleEffect.tscn");
		if (deathParticleScene is null) GD.PrintErr("ParticlePoolManager: Failed to load DeathParticleEffect.tscn");

		// Pre-warm tasks allow awaiting loading and instantiation concurrently if needed,
		// but for simplicity here we'll do them sequentially per pool type.
		// This async pattern mainly prevents blocking the main thread during loading.
		await InitializeSinglePoolAsync(damageParticleScene, targetParticlePoolSize, "Damage Particles");
		await InitializeSinglePoolAsync(deathParticleScene, targetParticlePoolSize, "Death Particles");

		poolsInitialized = true;
		initializationStarted = false;
		GD.Print("ParticlePoolManager: Initialization complete.");
	}

	private async Task InitializeSinglePoolAsync(PackedScene scene, int targetSize, string poolName)
	{
		if (scene is null)
		{
			GD.PrintErr($"ParticlePoolManager: Cannot initialize pool '{poolName}': Scene is null.");
			return;
		}

		// Ensure the queue exists
		if (!availableParticles.TryGetValue(scene, out var queue))
		{
			queue = new Queue<PooledParticleEffect>(targetSize);
			availableParticles.Add(scene, queue);
			GD.Print($"ParticlePoolManager: Created new queue for '{poolName}'.");
		}
		else
		{
			GD.Print($"ParticlePoolManager: Pool '{poolName}' for scene {scene.ResourcePath} already existed. Ensuring target size.");
		}

		int needed = targetSize - queue.Count;
		int createdCount = 0;
		GD.Print($"ParticlePoolManager: Pool '{poolName}' needs {needed} instances.");

		for (int i = 0; i < needed; i++)
		{
			// Yield every few instantiations if needed to prevent frame drops during heavy loading
			// if (i % 10 == 0) await Task.Delay(1); // Optional delay

			PooledParticleEffect instance = CreateAndSetupParticle(scene);
			if (instance is not null)
			{
				queue.Enqueue(instance);
				createdCount++;
			}
			// Allow engine to process events briefly
			await Task.Yield();
		}
		GD.Print($"ParticlePoolManager: - {poolName} Pool ({scene.ResourcePath}): {queue.Count}/{targetSize} (Added {createdCount})");
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
			GD.PrintErr($"ParticlePoolManager: Failed instantiate particle: {scene.ResourcePath}");
			return null;
		}

		particle.SourceScene = scene;
		particle.TopLevel = true; // Make particle independent of parent transform
		particle.Visible = false; // Start invisible
		particle.ProcessMode = ProcessModeEnum.Disabled; // Start disabled
		particle.ZIndex = ParticleZIndex;
		AddChild(particle); // Add to the manager node itself
		return particle;
	}

	public PooledParticleEffect GetParticleEffect(PackedScene scene, Vector2 globalPosition, Color? color = null)
	{
		if (scene is null)
		{
			GD.PrintErr("ParticlePoolManager.GetParticleEffect: Null scene provided.");
			return null;
		}

		if (!poolsInitialized)
		{
			GD.PushWarning("ParticlePoolManager.GetParticleEffect called before pools fully initialized!");
			// Fallback: Create a new one if not initialized, though this bypasses pooling benefits
			var emergencyParticle = CreateAndSetupParticle(scene);
			if (emergencyParticle is not null)
			{
				GD.PrintErr("ParticlePoolManager: Returning emergency particle instance.");
				SetupParticleInstance(emergencyParticle, globalPosition, color);
				emergencyParticle.PlayEffect(); // Must manually start
			}
			return emergencyParticle;
		}

		if (!availableParticles.TryGetValue(scene, out var queue))
		{
			GD.PrintErr($"ParticlePoolManager.GetParticleEffect: Pool not found for {scene.ResourcePath}! Should exist after initialization. Creating fallback instance.");
			var fallbackParticle = CreateAndSetupParticle(scene);
			if (fallbackParticle is not null)
			{
				SetupParticleInstance(fallbackParticle, globalPosition, color);
				fallbackParticle.PlayEffect();
			}
			return fallbackParticle;
		}

		PooledParticleEffect particle;
		if (queue.Count > 0)
		{
			particle = queue.Dequeue();
			// Validate instance before using
			if (particle is null || !IsInstanceValid(particle))
			{
				GD.PrintErr($"ParticlePoolManager: Invalid particle retrieved from pool {scene.ResourcePath}. Creating replacement.");
				particle = CreateAndSetupParticle(scene); // Create a new one if the pooled one was invalid
				if (particle is null) return null; // Check if creation failed
			}
		}
		else
		{
			GD.Print($"ParticlePoolManager: Pool empty for {scene.ResourcePath}! Creating new instance.");
			particle = CreateAndSetupParticle(scene);
			if (particle is null) return null; // Check if creation failed
		}

		SetupParticleInstance(particle, globalPosition, color);
		particle.PlayEffect(); // Start the particle effect logic
		return particle;
	}

	private void SetupParticleInstance(PooledParticleEffect particle, Vector2 globalPosition, Color? color)
	{
		particle.GlobalPosition = globalPosition;
		if (color.HasValue)
		{
			particle.Color = color.Value;
		}
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Pausable; // Enable processing, respecting pause state
		// ZIndex is set during CreateAndSetupParticle
	}

	public void ReturnParticleToPool(PooledParticleEffect particle)
	{
		// Basic validation
		if (particle is null || !IsInstanceValid(particle))
		{
			GD.PrintErr($"ParticlePoolManager.ReturnParticleToPool: Attempted to return an invalid particle instance.");
			return;
		}
		if (particle.SourceScene is null)
		{
			GD.PrintErr($"ParticlePoolManager: Particle {particle.GetInstanceId()} cannot return to pool: SourceScene is null. Freeing.");
			particle.QueueFree();
			return;
		}

		// Find the correct queue
		if (!availableParticles.TryGetValue(particle.SourceScene, out var queue))
		{
			GD.PrintErr($"ParticlePoolManager: Pool not found for {particle.SourceScene.ResourcePath} on return. Freeing particle {particle.GetInstanceId()}.");
			particle.QueueFree();
			return;
		}

		// Reset state for pooling
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		particle.Emitting = false; // Ensure emitting is stopped
		particle.GlobalPosition = Vector2.Zero; // Reset position

		queue.Enqueue(particle);
	}

	public void CleanUpActiveObjects()
	{
		GD.Print("ParticlePoolManager: Cleaning up active particles...");
		var nodesToClean = new List<PooledParticleEffect>();

		// Iterate through children of this manager node
		foreach (Node child in GetChildren())
		{
			if (child is PooledParticleEffect particle && particle.ProcessMode != ProcessModeEnum.Disabled)
			{
				nodesToClean.Add(particle);
			}
		}

		GD.Print($"ParticlePoolManager: Found {nodesToClean.Count} active particles to clean.");

		foreach (var particle in nodesToClean)
		{
			if (IsInstanceValid(particle)) // Double-check validity
			{
				GD.Print($" - Returning active particle {particle.GetInstanceId()} to pool.");
				ReturnParticleToPool(particle);
			}
		}
		GD.Print("ParticlePoolManager: Finished cleaning active particles.");
	}
}
