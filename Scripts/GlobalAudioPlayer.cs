using Godot;

namespace CosmocrushGD;

public partial class GlobalAudioPlayer : Node
{
	private static GlobalAudioPlayer _instance;
	public static GlobalAudioPlayer Instance => _instance;

	public AudioStream UiSound = ResourceLoader.Load<AudioStream>("res://Audio/SFX/Ui.mp3");

	private const string SfxBusName = "SFX";

	public override void _EnterTree()
	{
		if (_instance is not null)
		{
			QueueFree();
			return;
		}

		_instance = this;
	}

	public void PlaySound2D(AudioStream stream, Vector2 position = default, float volumeDb = 0f)
	{
		AudioStreamPlayer2D audioPlayer = new()
		{
			Stream = stream,
			Position = position,
			VolumeDb = volumeDb,
			Bus = SfxBusName
		};

		AddChild(audioPlayer);
		audioPlayer.Play();
		audioPlayer.Finished += () => CleanupAudioPlayer(audioPlayer);
	}

	public void PlaySound(AudioStream stream, float volumeDb = 0f)
	{
		AudioStreamPlayer audioPlayer = new()
		{
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = SfxBusName
		};

		AddChild(audioPlayer);
		audioPlayer.Play();
		audioPlayer.Finished += () => CleanupAudioPlayer(audioPlayer);
	}

	private static void CleanupAudioPlayer(Node audioPlayer)
	{
		audioPlayer.QueueFree();
	}
}
