using Godot;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	private static GlobalAudioPlayer _instance;
	public static GlobalAudioPlayer Instance => _instance;

	public AudioStream UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");

	public override void _EnterTree()
	{
		if (_instance != null)
		{
			QueueFree();
			return;
		}
		_instance = this;
	}

	public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
	{
		var audioPlayer = new AudioStreamPlayer2D
		{
			Stream = stream,
			Position = position,
			VolumeDb = volumeDb
		};

		AddChild(audioPlayer);
		audioPlayer.Play();
		audioPlayer.Finished += () => CleanupAudioPlayer(audioPlayer);
	}

	public void PlaySound(AudioStream stream, float volumeDb = 0f)
	{
		var audioPlayer = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb
		};

		AddChild(audioPlayer);
		audioPlayer.Play();
		audioPlayer.Finished += () => CleanupAudioPlayer(audioPlayer);
	}

	private void CleanupAudioPlayer(Node audioPlayer)
	{
		audioPlayer.QueueFree();
	}
}
