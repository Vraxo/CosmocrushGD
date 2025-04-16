using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : CenterContainer
{
	[Export] private Label titleLabel;
	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;
	[Export] private Label musicLabel;
	[Export] private Label sfxLabel;
	[Export] private Button applyButton;
	[Export] private Button returnButton;

	private float previousMusicVolume;
	private float previousSfxVolume;

	private const float FadeInDuration = 0.15f;
	private const float StaggerDelay = 0.075f;

	private MenuShell menuShell;

	public override void _Ready()
	{
		menuShell = GetParent()?.GetParent<MenuShell>();
		if (menuShell is null)
		{
			GD.PrintErr("SettingsMenu: Could not find MenuShell parent!");
		}

		bool initializationFailed = false;

		if (titleLabel is null) { GD.PrintErr("SettingsMenu: Title Label not assigned!"); initializationFailed = true; }
		if (musicSlider is null) { GD.PrintErr("SettingsMenu: Music Slider not assigned!"); initializationFailed = true; }
		if (sfxSlider is null) { GD.PrintErr("SettingsMenu: SFX Slider not assigned!"); initializationFailed = true; }
		if (musicLabel is null) { GD.PrintErr("SettingsMenu: Music Label not assigned!"); initializationFailed = true; }
		if (sfxLabel is null) { GD.PrintErr("SettingsMenu: SFX Label not assigned!"); initializationFailed = true; }
		if (applyButton is null) { GD.PrintErr("SettingsMenu: Apply Button not assigned!"); initializationFailed = true; }
		if (returnButton is null) { GD.PrintErr("SettingsMenu: Return Button not assigned!"); initializationFailed = true; }

		if (initializationFailed)
		{
			GD.PrintErr("SettingsMenu: Initialization failed due to missing node references. Aborting setup.");
			return;
		}

		musicSlider.Value = Settings.Instance.SettingsData.MusicVolume;
		sfxSlider.Value = Settings.Instance.SettingsData.SfxVolume;

		CapturePreviousVolumes();

		applyButton.Pressed += OnApplyButtonPressed;
		returnButton.Pressed += OnReturnButtonPressed;

		musicSlider.ValueChanged += OnSliderValueChanged;
		sfxSlider.ValueChanged += OnSliderValueChanged;

		SetInitialAlphas();
		CallDeferred(nameof(StartFadeInAnimation));
		UpdateApplyAvailability();
	}

	private void SetInitialAlphas()
	{
		if (titleLabel is not null) titleLabel.Modulate = Colors.Transparent;
		if (musicLabel is not null) musicLabel.Modulate = Colors.Transparent;
		if (musicSlider is not null) musicSlider.Modulate = Colors.Transparent;
		if (sfxLabel is not null) sfxLabel.Modulate = Colors.Transparent;
		if (sfxSlider is not null) sfxSlider.Modulate = Colors.Transparent;
		if (applyButton is not null) applyButton.Modulate = Colors.Transparent;
		if (returnButton is not null) returnButton.Modulate = Colors.Transparent;
	}

	private void StartFadeInAnimation()
	{
		if (titleLabel is null || musicLabel is null || musicSlider is null ||
			sfxLabel is null || sfxSlider is null || applyButton is null || returnButton is null)
		{
			return;
		}

		Tween tween = CreateTween();
		tween.SetParallel(false);

		tween.TweenInterval(StaggerDelay);

		tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenInterval(StaggerDelay);

		tween.SetParallel(true);
		tween.TweenProperty(musicLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(musicSlider, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.SetParallel(false);
		tween.TweenInterval(StaggerDelay);

		tween.SetParallel(true);
		tween.TweenProperty(sfxLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(sfxSlider, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.SetParallel(false);
		tween.TweenInterval(StaggerDelay);

		tween.SetParallel(true);
		tween.TweenProperty(applyButton, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);

		tween.Play();
	}


	private void OnApplyButtonPressed()
	{
		if (musicSlider is not null && sfxSlider is not null)
		{
			Settings.Instance.SettingsData.MusicVolume = musicSlider.Value;
			Settings.Instance.SettingsData.SfxVolume = sfxSlider.Value;
			Settings.Instance.Save();

			CapturePreviousVolumes();
			UpdateApplyAvailability();
		}
	}

	private void OnReturnButtonPressed()
	{
		menuShell?.ShowMainMenu();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			OnReturnButtonPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void CapturePreviousVolumes()
	{
		if (musicSlider is not null)
		{
			previousMusicVolume = (float)musicSlider.Value;
		}
		if (sfxSlider is not null)
		{
			previousSfxVolume = (float)sfxSlider.Value;
		}
	}

	private void UpdateApplyAvailability()
	{
		if (applyButton is not null && musicSlider is not null && sfxSlider is not null)
		{
			bool musicChanged = !Mathf.IsEqualApprox((float)musicSlider.Value, previousMusicVolume, 0.001f);
			bool sfxChanged = !Mathf.IsEqualApprox((float)sfxSlider.Value, previousSfxVolume, 0.001f);
			applyButton.Disabled = !(musicChanged || sfxChanged);
		}
	}

	private void OnSliderValueChanged(double value)
	{
		UpdateApplyAvailability();
	}

	public override void _ExitTree()
	{
		if (applyButton is not null && IsInstanceValid(applyButton))
		{
			applyButton.Pressed -= OnApplyButtonPressed;
		}
		if (returnButton is not null && IsInstanceValid(returnButton))
		{
			returnButton.Pressed -= OnReturnButtonPressed;
		}
		if (musicSlider is not null && IsInstanceValid(musicSlider))
		{
			musicSlider.ValueChanged -= OnSliderValueChanged;
		}
		if (sfxSlider is not null && IsInstanceValid(sfxSlider))
		{
			sfxSlider.ValueChanged -= OnSliderValueChanged;
		}

		base._ExitTree();
	}
}
