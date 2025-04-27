using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	public static GlobalAudioPlayer Instance { get; private set; }
	public AudioStream UiSound { get; private set; }

	// Removed PackedScene references for particles, indicators, projectiles
	// Removed pool size variables for non-audio objects
	private const int TargetAudioPoolSize = 10;
	private const string SfxBusName = "SFX";

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();
	// Removed Dictionaries/Queues for particles, indicators, projectiles
	// Removed gameplayPoolsInitialized and initializationStarted flags

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
		// Removed call to InitializeGameplayPoolsAsync
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

	// Removed InitializeGameplayPoolsAsync method
	// Removed InitializeSinglePool<T> method (specific versions are in new managers)
	// Removed CreateAndSetupParticle, CreateAndSetupIndicator, CreateAndSetupProjectile methods

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

		if (!IsInstanceValid(audioPlayer)) // Check if the instance is valid before using
		{
			GD.PrintErr("GlobalAudioPlayer: Dequeued invalid AudioStreamPlayer2D instance. Creating new one.");
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

		if (!IsInstanceValid(audioPlayer)) // Check if the instance is valid before using
		{
			GD.PrintErr("GlobalAudioPlayer: Dequeued invalid AudioStreamPlayer instance. Creating new one.");
			audioPlayer = CreateAndSetupPlayer1D();
		}

		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.Play();
	}

	// Removed GetParticleEffect method
	// Removed GetDamageIndicator method
	// Removed GetProjectile method

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

	// Removed ReturnParticleToPool method
	// Removed ReturnIndicatorToPool method
	// Removed ReturnProjectileToPool method
	// Removed CleanUpActiveGameObjects method (responsibility moved)
}
