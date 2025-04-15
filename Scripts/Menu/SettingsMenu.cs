using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : ColorRect
{
	private PackedScene mainMenuScene = ResourceLoader.Load<PackedScene>("res://Scenes/Menu/NewMainMenu.tscn");

	// Add TitleLabel export
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

	public override void _Ready()
	{
		bool initializationFailed = false;

		// Explicitly check each exported node
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
			return; // Prevent further errors
		}

		// Proceed only if all nodes are valid
		musicSlider.Value = Settings.Instance.SettingsData.MusicVolume;
		sfxSlider.Value = Settings.Instance.SettingsData.SfxVolume;

		CapturePreviousVolumes();

		applyButton.Pressed += OnApplyButtonPressed;
		returnButton.Pressed += OnReturnButtonPressed;

		SetInitialAlphas();
		CallDeferred(nameof(StartFadeInAnimation));
		UpdateApplyAvailability(); // Set initial button state
	}

	private void SetInitialAlphas()
	{
		// Null checks are technically redundant now due to _Ready check, but safe to keep
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
		// Check again if nodes are valid before starting tween
		if (titleLabel is null || musicLabel is null || musicSlider is null ||
			sfxLabel is null || sfxSlider is null || applyButton is null || returnButton is null)
		{
			GD.PrintErr("SettingsMenu: Cannot start animation, one or more nodes are null.");
			return;
		}

		Tween tween = CreateTween();
		tween.SetParallel(false); // Run sequentially by default

		tween.TweenInterval(StaggerDelay);

		// Animate Title
		tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenInterval(StaggerDelay);

		// Animate Music Label and Slider together
		tween.SetParallel(true);
		tween.TweenProperty(musicLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(musicSlider, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.SetParallel(false);
		tween.TweenInterval(StaggerDelay);


		// Animate SFX Label and Slider together
		tween.SetParallel(true);
		tween.TweenProperty(sfxLabel, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(sfxSlider, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.SetParallel(false);
		tween.TweenInterval(StaggerDelay);


		// Animate Buttons together (or sequentially if preferred)
		tween.SetParallel(true); // Example: Animate buttons together
		tween.TweenProperty(applyButton, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration)
			 .SetEase(Tween.EaseType.Out);
		// No final stagger needed if buttons animate together

		tween.Play();
	}


	private void OnApplyButtonPressed()
	{
		if (musicSlider is not null && sfxSlider is not null)
		{
			Settings.Instance.SettingsData.MusicVolume = musicSlider.Value; // Use double directly
			Settings.Instance.SettingsData.SfxVolume = sfxSlider.Value;   // Use double directly
			Settings.Instance.Save();

			CapturePreviousVolumes();
			UpdateApplyAvailability();
		}
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
	}

	private void OnReturnButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);

		if (mainMenuScene is not null)
		{
			GetTree().ChangeSceneToPacked(mainMenuScene);
		}
		else
		{
			GD.PrintErr("MainMenuScene is not loaded!");
			// Fallback or error handling
			GetTree().ChangeSceneToFile("res://Scenes/Menu/NewMainMenu.tscn");
		}
	}

	public override void _Process(double delta)
	{
		// Layout is now handled by containers.
		// We only need _Process if we need to check Apply button state continuously,
		// but it's better to check it only when slider values change.
		// Let's connect the ValueChanged signals instead.

		// Remove UpdateApplyAvailability() from here if connecting signals.
		// UpdateApplyAvailability(); // Keep if NOT connecting signals
	}

	public override void _Input(InputEvent @event)
	{
		// Add ui_cancel handling to go back
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
			// Use a small tolerance for float comparison
			bool musicChanged = !Mathf.IsEqualApprox((float)musicSlider.Value, previousMusicVolume, 0.001f);
			bool sfxChanged = !Mathf.IsEqualApprox((float)sfxSlider.Value, previousSfxVolume, 0.001f);
			applyButton.Disabled = !(musicChanged || sfxChanged);
		}
	}

	// Optional: Connect slider ValueChanged signals for better performance
	// than checking in _Process. Requires adding these methods and connecting
	// the signals in _Ready after checking nodes are not null.
	// private void OnMusicSliderChanged(double value)
	// {
	//     UpdateApplyAvailability();
	// }
	// private void OnSfxSliderChanged(double value)
	// {
	//     UpdateApplyAvailability();
	// }

	public override void _ExitTree()
	{
		// Disconnect signals if they were connected
		if (applyButton is not null && IsInstanceValid(applyButton))
		{
			var callable = Callable.From(OnApplyButtonPressed);
			if (applyButton.IsConnected(Button.SignalName.Pressed, callable))
			{
				applyButton.Disconnect(Button.SignalName.Pressed, callable);
			}
		}
		if (returnButton is not null && IsInstanceValid(returnButton))
		{
			var callable = Callable.From(OnReturnButtonPressed);
			if (returnButton.IsConnected(Button.SignalName.Pressed, callable))
			{
				returnButton.Disconnect(Button.SignalName.Pressed, callable);
			}
		}

		// Disconnect slider signals if you added them
		// if (musicSlider is not null && IsInstanceValid(musicSlider))
		// {
		//     var callable = Callable.From<double>(OnMusicSliderChanged);
		//     if (musicSlider.IsConnected(HSlider.SignalName.ValueChanged, callable))
		//     {
		//         musicSlider.Disconnect(HSlider.SignalName.ValueChanged, callable);
		//     }
		// }
		// // ... same for sfxSlider ...

		base._ExitTree();
	}
}
