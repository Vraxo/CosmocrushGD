using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : Node2D
{
	private PackedScene mainMenuScene = ResourceLoader.Load<PackedScene>("res://Scenes/Menu/MainMenu.tscn");

	[Export] private HSlider masterSlider;
	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;

	[Export] private Label masterLabel;
	[Export] private Label musicLabel;
	[Export] private Label sfxLabel;

	[Export] private Button applyButton;
	[Export] private Button returnButton;

	private float previousMasterVolume;
	private float previousMusicVolume;
	private float previousSfxVolume;

	public override void _Ready()
	{
		masterSlider.Value = Settings.Instance.SettingsData.MasterVolume;
		musicSlider.Value = Settings.Instance.SettingsData.MusicVolume;
		sfxSlider.Value = Settings.Instance.SettingsData.SfxVolume;

		CapturePreviousVolumes();

		applyButton.Pressed += OnApplyButtonPressed;
		returnButton.Pressed += OnReturnButtonPressed;
	}

	private void OnApplyButtonPressed()
	{
		Settings.Instance.SettingsData.MasterVolume = (float)masterSlider.Value;
		Settings.Instance.SettingsData.MusicVolume = (float)musicSlider.Value;
		Settings.Instance.SettingsData.SfxVolume = (float)sfxSlider.Value;
		Settings.Instance.Save();

		CapturePreviousVolumes();
	}

	private void OnReturnButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);

		var mainMenu = mainMenuScene.Instantiate<Node2D>();
		GetParent().AddChild(mainMenu);
		QueueFree();
	}

	public override void _Process(double delta)
	{
		UpdateLabels();
		UpdateButtons();
		UpdateApplyAvailability();
	}

	private void CapturePreviousVolumes()
	{
		previousMasterVolume = (float)masterSlider.Value;
		previousMusicVolume = (float)musicSlider.Value;
		previousSfxVolume = (float)sfxSlider.Value;
	}

	private void UpdateApplyAvailability()
	{
		applyButton.Disabled = (float)masterSlider.Value == previousMasterVolume &&
							  (float)musicSlider.Value == previousMusicVolume &&
							  (float)sfxSlider.Value == previousSfxVolume;
	}

	private void UpdateLabels()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();

		masterLabel.Position = new(
			windowSize.X / 2 - (masterLabel.Size.X + masterSlider.Size.X) / 2,
			windowSize.Y / 2 - 50
		);

		musicLabel.Position = new(
			windowSize.X / 2 - (masterLabel.Size.X + musicSlider.Size.X) / 2,
			windowSize.Y / 2
		);

		sfxLabel.Position = new(
			windowSize.X / 2 - (masterLabel.Size.X + sfxSlider.Size.X) / 2,
			windowSize.Y / 2 + 50
		);
	}

	private void UpdateButtons()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();

		float buttonSpacing = 20f;

		applyButton.Position = new(
			(windowSize.X / 2) - applyButton.Size.X - buttonSpacing,
			windowSize.Y / 2 + 100
		);

		returnButton.Position = new(
			(windowSize.X / 2) + buttonSpacing,
			windowSize.Y / 2 + 100
		);
	}
}
