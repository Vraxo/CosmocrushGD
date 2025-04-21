using Godot;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	private static GlobalAudioPlayer _instance;
	public static GlobalAudioPlayer Instance => _instance;

	public AudioStream UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");

	private PackedScene damageParticleScene;
	private PackedScene deathParticleScene;
	private PackedScene damageIndicatorScene;

	private int initialParticlePoolSize = 20;
	private int initialIndicatorPoolSize = 30;
	private int initialProjectilePoolSize = 20;

	private const string SfxBusName = "SFX";
	private const int InitialAudioPoolSize = 10;

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
	private Dictionary<PackedScene, Queue<PooledParticleEffect>> availableParticles = new();
	private Queue<DamageIndicator> availableIndicators = new();
	private Dictionary<PackedScene, Queue<Projectile>> availableProjectiles = new();


	public override void _EnterTree()
	{
		if (_instance is not null)
		{
			QueueFree();
			return;
		}
		_instance = this;
		ProcessMode = ProcessModeEnum.Always;

		damageParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageParticleEffect.tscn");
		deathParticleScene = ResourceLoader.Load<PackedScene>("res://Scenes/DeathParticleEffect.tscn");
		damageIndicatorScene = ResourceLoader.Load<PackedScene>("res://Scenes/DamageIndicator.tscn");

		if (damageParticleScene is null) GD.PrintErr("Failed to load DamageParticleEffect.tscn");
		if (deathParticleScene is null) GD.PrintErr("Failed to load DeathParticleEffect.tscn");
		if (damageIndicatorScene is null) GD.PrintErr("Failed to load DamageIndicator.tscn");

		InitializeAudioPools();
		InitializeParticlePools();
		InitializeIndicatorPool();
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
		if (damageParticleScene is not null) InitializeSingleParticlePool(damageParticleScene);
		else GD.PrintErr("GlobalAudioPlayer: Damage Particle Scene could not be loaded!");

		if (deathParticleScene is not null) InitializeSingleParticlePool(deathParticleScene);
		else GD.PrintErr("GlobalAudioPlayer: Death Particle Scene could not be loaded!");
	}

	private void InitializeSingleParticlePool(PackedScene particleScene)
	{
		if (particleScene is null)
		{
			GD.PrintErr("InitializeSingleParticlePool: Null scene.");
			return;
		}
		if (!availableParticles.ContainsKey(particleScene))
		{
			availableParticles.Add(particleScene, new Queue<PooledParticleEffect>());
		}
		var queue = availableParticles[particleScene];
		int createdCount = 0;
		for (int i = 0; i < initialParticlePoolSize; i++)
		{
			var particle = CreateAndSetupParticle(particleScene);
			if (particle is not null) { queue.Enqueue(particle); createdCount++; }
		}
		GD.Print($"Initialized particle pool for {particleScene.ResourcePath}. Added {createdCount}. Total: {queue.Count}. Target: {initialParticlePoolSize}");
	}

	private void InitializeIndicatorPool()
	{
		if (damageIndicatorScene is null)
		{
			GD.PrintErr("GlobalAudioPlayer: Damage Indicator Scene null!");
			return;
		}
		int createdCount = 0;
		for (int i = 0; i < initialIndicatorPoolSize; i++)
		{
			var indicator = CreateAndSetupIndicator();
			if (indicator is not null) { availableIndicators.Enqueue(indicator); createdCount++; }
		}
		GD.Print($"Initialized indicator pool. Added {createdCount}. Total: {availableIndicators.Count}. Target: {initialIndicatorPoolSize}");
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
		PooledParticleEffect particle = scene.Instantiate<PooledParticleEffect>();
		if (particle is null) { GD.PrintErr($"Failed instantiate particle: {scene.ResourcePath}"); return null; }
		particle.SourceScene = scene;
		particle.TopLevel = true; // Particles should also be TopLevel
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(particle); // Initially add to self (pool manager)
		return particle;
	}

	private DamageIndicator CreateAndSetupIndicator()
	{
		if (damageIndicatorScene is null) { GD.PrintErr("Indicator scene null."); return null; }
		DamageIndicator indicator = damageIndicatorScene.Instantiate<DamageIndicator>();
		if (indicator is null) { GD.PrintErr($"Failed instantiate indicator: {damageIndicatorScene.ResourcePath}"); return null; }
		indicator.SourceScene = damageIndicatorScene;
		indicator.TopLevel = true; // Indicators should also be TopLevel
		indicator.Visible = false;
		indicator.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(indicator); // Initially add to self
		return indicator;
	}

	private Projectile CreateAndSetupProjectile(PackedScene scene)
	{
		if (scene is null) { GD.PrintErr("Cannot create projectile: scene is null."); return null; }
		Projectile projectile = scene.Instantiate<Projectile>();
		if (projectile is null) { GD.PrintErr($"Failed to instantiate projectile from scene: {scene.ResourcePath}"); return null; }
		projectile.SourceScene = scene;
		projectile.TopLevel = true; // Make projectile independent of parent transform
		projectile.Visible = false;
		projectile.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(projectile); // Initially add to self
		return projectile;
	}

	public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
	{
		if (stream is null) return;
		AudioStreamPlayer2D audioPlayer;
		if (availablePlayers2D.Count > 0) audioPlayer = availablePlayers2D.Dequeue();
		else audioPlayer = CreateAndSetupPlayer2D();
		// Player is already child of this node, position will be relative unless TopLevel=true (which it isn't)
		// For 2D sounds, GlobalPosition is better if the source might move (like the player)
		audioPlayer.GlobalPosition = position;
		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.Play();
	}

	public void PlaySound(AudioStream stream, float volumeDb = 0f)
	{
		if (stream is null) return;
		AudioStreamPlayer audioPlayer;
		if (availablePlayers1D.Count > 0) audioPlayer = availablePlayers1D.Dequeue();
		else audioPlayer = CreateAndSetupPlayer1D();
		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.Play();
	}

	public PooledParticleEffect GetParticleEffect(PackedScene scene, Vector2 position, Color? color = null)
	{
		if (scene is null) { GD.PrintErr("GetParticleEffect: Null scene."); return null; }
		if (!availableParticles.TryGetValue(scene, out Queue<PooledParticleEffect> queue))
		{
			// Fallback: Create, DO NOT add to pool management, parent to root
			GD.PrintErr($"GetParticleEffect: Pool not found for {scene.ResourcePath}. Creating fallback.");
			var newParticle = CreateAndSetupParticle(scene); // Will set TopLevel=true
			if (newParticle is not null)
			{
				// Remove from pool manager, add to root (necessary for first time?)
				// This seems counter-intuitive now with TopLevel. Let's keep it child of pool manager.
				// newParticle.GetParent()?.RemoveChild(newParticle);
				// GetTree().Root.AddChild(newParticle);
				newParticle.GlobalPosition = position;
				newParticle.ProcessMode = ProcessModeEnum.Inherit;
				newParticle.Visible = true;
				if (color.HasValue) newParticle.Color = color.Value;
				newParticle.PlayEffect();
				return newParticle;
			}
			return null;
		}
		PooledParticleEffect particle;
		if (queue.Count > 0)
		{
			particle = queue.Dequeue();
			if (particle is null || !IsInstanceValid(particle)) { GD.PrintErr("Invalid particle in pool."); particle = CreateAndSetupParticle(scene); }
		}
		else
		{
			GD.Print($"Particle pool empty for {scene.ResourcePath}. Creating new.");
			particle = CreateAndSetupParticle(scene);
		}
		if (particle is null) return null;

		// No reparenting needed due to TopLevel = true
		// if (particle.GetParent() != this) { ... }

		particle.GlobalPosition = position; // Set global position directly
		if (color.HasValue) particle.Color = color.Value;
		particle.Visible = true;
		particle.ProcessMode = ProcessModeEnum.Inherit;
		particle.PlayEffect();
		return particle;
	}

	public DamageIndicator GetDamageIndicator()
	{
		if (damageIndicatorScene is null) { GD.PrintErr("Indicator scene null."); return null; }
		DamageIndicator indicator;
		if (availableIndicators.Count > 0)
		{
			indicator = availableIndicators.Dequeue();
			if (indicator is null || !IsInstanceValid(indicator)) { GD.PrintErr("Invalid indicator in pool."); indicator = CreateAndSetupIndicator(); }
		}
		else
		{
			GD.Print("Indicator pool empty. Creating new.");
			indicator = CreateAndSetupIndicator();
		}
		if (indicator is null) return null;

		// No reparenting needed due to TopLevel = true.
		// Parent (enemy) will set GlobalPosition in its Setup call.
		indicator.Visible = true;
		indicator.ProcessMode = ProcessModeEnum.Inherit;
		indicator.Modulate = Colors.White;
		indicator.AnimatedAlpha = 1.0f;
		indicator.Scale = Vector2.One;
		// Position is set by the caller via indicator.Setup() or setting GlobalPosition
		return indicator;
	}

	public Projectile GetProjectile(PackedScene scene)
	{
		if (scene is null) { GD.PrintErr("GetProjectile: Null scene."); return null; }

		if (!availableProjectiles.TryGetValue(scene, out Queue<Projectile> queue))
		{
			GD.Print($"GetProjectile: Creating new pool for scene {scene.ResourcePath}.");
			queue = new Queue<Projectile>();
			availableProjectiles.Add(scene, queue);
			int createdCount = 0;
			for (int i = 0; i < initialProjectilePoolSize; i++)
			{
				var proj = CreateAndSetupProjectile(scene);
				if (proj is not null) { queue.Enqueue(proj); createdCount++; }
			}
			GD.Print($"Initialized projectile pool for {scene.ResourcePath}. Added {createdCount}. Target: {initialProjectilePoolSize}");
		}

		Projectile projectile;
		if (queue.Count > 0)
		{
			projectile = queue.Dequeue();
			if (projectile is null || !IsInstanceValid(projectile)) { GD.PrintErr("Invalid projectile in pool."); projectile = CreateAndSetupProjectile(scene); }
		}
		else
		{
			GD.Print($"Projectile pool empty for {scene.ResourcePath}. Creating new.");
			projectile = CreateAndSetupProjectile(scene);
		}
		if (projectile is null) return null;

		// No reparenting needed due to TopLevel = true
		// Caller sets GlobalPosition via projectile.Setup()

		projectile.Visible = true;
		projectile.ProcessMode = ProcessModeEnum.Inherit;

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
		// Reset GlobalPosition? Probably not needed as it's set on reuse.
		// audioPlayer.GlobalPosition = Vector2.Zero;
		availablePlayers2D.Enqueue(audioPlayer);
	}

	public void ReturnParticleToPool(PooledParticleEffect particle, PackedScene sourceScene)
	{
		if (particle is null || !IsInstanceValid(particle) || sourceScene is null) { particle?.QueueFree(); return; }
		if (!availableParticles.TryGetValue(sourceScene, out Queue<PooledParticleEffect> queue))
		{
			GD.PrintErr($"Particle pool not found for {sourceScene.ResourcePath}. Freeing.");
			particle.QueueFree();
			return;
		}
		particle.Visible = false;
		particle.ProcessMode = ProcessModeEnum.Disabled;
		particle.Emitting = false;
		// No need to reset GlobalPosition if TopLevel=true and it's set on reuse
		// particle.GlobalPosition = Vector2.Zero;
		// No need to reparent if it stays child of this node
		// if (particle.GetParent() != this) { particle.GetParent()?.RemoveChild(particle); AddChild(particle); }
		queue.Enqueue(particle);
	}

	public void ReturnIndicatorToPool(DamageIndicator indicator)
	{
		if (indicator is null || !IsInstanceValid(indicator)) return;
		indicator.Visible = false;
		indicator.ProcessMode = ProcessModeEnum.Disabled;
		indicator.ResetForPooling();
		// No need to reparent
		// if (indicator.GetParent() != this) { indicator.GetParent()?.RemoveChild(indicator); AddChild(indicator); }
		availableIndicators.Enqueue(indicator);
	}

	public void ReturnProjectileToPool(Projectile projectile, PackedScene sourceScene)
	{
		if (projectile is null || !IsInstanceValid(projectile) || sourceScene is null) { projectile?.QueueFree(); return; }
		if (!availableProjectiles.TryGetValue(sourceScene, out Queue<Projectile> queue))
		{
			GD.PrintErr($"Projectile pool not found for {sourceScene.ResourcePath}. Freeing.");
			projectile.QueueFree();
			return;
		}
		projectile.Visible = false;
		projectile.ProcessMode = ProcessModeEnum.Disabled;
		projectile.ResetForPooling();
		// No need to reparent
		// if (projectile.GetParent() != this) { projectile.GetParent()?.RemoveChild(projectile); AddChild(projectile); }
		queue.Enqueue(projectile);
	}
}
