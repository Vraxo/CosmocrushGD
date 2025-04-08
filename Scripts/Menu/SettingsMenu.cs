using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : ColorRect
{
	private PackedScene mainMenuScene = ResourceLoader.Load<PackedScene>("res://Scenes/Menu/NewMainMenu.tscn");

	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;

	[Export] private Label musicLabel;
	[Export] private Label sfxLabel;

	[Export] private Button applyButton;
	[Export] private Button returnButton;

	private float previousMusicVolume;
	private float previousSfxVolume;

	public override void _Ready()
	{
		musicSlider.Value = Settings.Instance.SettingsData.MusicVolume;
		sfxSlider.Value = Settings.Instance.SettingsData.SfxVolume;

		CapturePreviousVolumes();

		applyButton.Pressed += OnApplyButtonPressed;
		returnButton.Pressed += OnReturnButtonPressed;
	}

	private void OnApplyButtonPressed()
	{
		Settings.Instance.SettingsData.MusicVolume = (float)musicSlider.Value;
		Settings.Instance.SettingsData.SfxVolume = (float)sfxSlider.Value;
		Settings.Instance.Save();

		CapturePreviousVolumes();
	}

	private void OnReturnButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);

		GetTree().ChangeSceneToFile("res://Scenes/Menu/NewMainMenu.tscn");
	}

	public override void _Process(double delta)
	{
		UpdateLabels();
		UpdateButtons();
		UpdateApplyAvailability();
	}

	private void CapturePreviousVolumes()
	{
		previousMusicVolume = (float)musicSlider.Value;
		previousSfxVolume = (float)sfxSlider.Value;
	}

	private void UpdateApplyAvailability()
	{
		applyButton.Disabled = (float)musicSlider.Value == previousMusicVolume &&
							  (float)sfxSlider.Value == previousSfxVolume;
	}

	private void UpdateLabels()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();

		musicLabel.Position = new(
			windowSize.X / 2 - (musicLabel.Size.X + musicSlider.Size.X) / 2,
			windowSize.Y / 2 - 50
		);

		sfxLabel.Position = new(
			windowSize.X / 2 - (sfxLabel.Size.X + sfxSlider.Size.X) / 2,
			windowSize.Y / 2
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
			windowSize.X / 2 + buttonSpacing,
			windowSize.Y / 2 + 100
		);
	}
}
