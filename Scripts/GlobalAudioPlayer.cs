using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	private static GlobalAudioPlayer _instance;
	public static GlobalAudioPlayer Instance => _instance;

	public AudioStream UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");

	private const string SfxBusName = "SFX";
	private const int InitialPoolSize = 10;

	private Queue<AudioStreamPlayer> availablePlayers1D = new();
	private Queue<AudioStreamPlayer2D> availablePlayers2D = new();

	public override void _EnterTree()
	{
		if (_instance is not null)
		{
			QueueFree();
			return;
		}
		_instance = this;
		ProcessMode = ProcessModeEnum.Always;
		InitializePools();
	}

	private void InitializePools()
	{
		for (int i = 0; i < InitialPoolSize; i++)
		{
			availablePlayers1D.Enqueue(CreateAndSetupPlayer1D());
			availablePlayers2D.Enqueue(CreateAndSetupPlayer2D());
		}
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
			GD.Print("GlobalAudioPlayer: 2D pool empty, creating new player.");
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
			GD.Print("GlobalAudioPlayer: 1D pool empty, creating new player.");
			audioPlayer = CreateAndSetupPlayer1D();
		}

		audioPlayer.Stream = stream;
		audioPlayer.VolumeDb = volumeDb;
		audioPlayer.Play();
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
}
