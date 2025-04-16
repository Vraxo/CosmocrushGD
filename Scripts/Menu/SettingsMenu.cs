using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : CenterContainer
{
	[Export] private Label titleLabel;
	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;
	[Export] private Label musicLabel;
	[Export] private Label sfxLabel;
	[Export] private UIButton applyButton;
	[Export] private UIButton returnButton;

	private float previousMusicVolume;
	private float previousSfxVolume;

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.1f;
	private const float InitialScaleMultiplier = 2.0f;

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

		CallDeferred(nameof(SetupPivots));

		SetInitialState();
		CallDeferred(nameof(StartFadeInAnimation));
		UpdateApplyAvailability();
	}

	private void SetupPivots()
	{
		if (titleLabel is not null)
		{
			titleLabel.PivotOffset = titleLabel.Size / 2;
		}
		if (musicLabel is not null)
		{
			musicLabel.PivotOffset = musicLabel.Size / 2;
		}
		if (sfxLabel is not null)
		{
			sfxLabel.PivotOffset = sfxLabel.Size / 2;
		}
		if (musicSlider is not null)
		{
			musicSlider.PivotOffset = musicSlider.Size / 2;
		}
		if (sfxSlider is not null)
		{
			sfxSlider.PivotOffset = sfxSlider.Size / 2;
		}
		if (applyButton is not null)
		{
			applyButton.PivotOffset = applyButton.Size / 2;
		}
		if (returnButton is not null)
		{
			returnButton.PivotOffset = returnButton.Size / 2;
		}
	}

	private void SetInitialState()
	{
		Vector2 initialScale = Vector2.One;

		if (titleLabel is not null)
		{
			titleLabel.Modulate = Colors.Transparent;
			titleLabel.Scale = initialScale;
		}
		if (musicLabel is not null)
		{
			musicLabel.Modulate = Colors.Transparent;
			musicLabel.Scale = initialScale;
		}
		if (musicSlider is not null)
		{
			musicSlider.Modulate = Colors.Transparent;
			musicSlider.Scale = initialScale;
		}
		if (sfxLabel is not null)
		{
			sfxLabel.Modulate = Colors.Transparent;
			sfxLabel.Scale = initialScale;
		}
		if (sfxSlider is not null)
		{
			sfxSlider.Modulate = Colors.Transparent;
			sfxSlider.Scale = initialScale;
		}
		if (applyButton is not null)
		{
			applyButton.Modulate = Colors.Transparent;
			applyButton.Scale = initialScale;
			applyButton.TweenScale = false;
		}
		if (returnButton is not null)
		{
			returnButton.Modulate = Colors.Transparent;
			returnButton.Scale = initialScale;
			returnButton.TweenScale = false;
		}
	}

	private void StartFadeInAnimation()
	{
		if (titleLabel is null || musicLabel is null || musicSlider is null ||
			sfxLabel is null || sfxSlider is null || applyButton is null || returnButton is null)
		{
			return;
		}

		// Ensure pivots are set before animating, called deferred but good practice
		SetupPivots();

		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		Vector2 initialScaleValue = Vector2.One * InitialScaleMultiplier;
		Vector2 finalScale = Vector2.One;

		tween.TweenInterval(StaggerDelay);

		if (titleLabel is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(titleLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (musicLabel is not null && musicSlider is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(musicLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(musicLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue); // Enabled scale
			tween.TweenProperty(musicSlider, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(musicSlider, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (sfxLabel is not null && sfxSlider is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(sfxLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(sfxLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue); // Enabled scale
			tween.TweenProperty(sfxSlider, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(sfxSlider, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (applyButton is not null && returnButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(applyButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(applyButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(() =>
			{
				if (applyButton is not null) { applyButton.TweenScale = true; }
				if (returnButton is not null) { returnButton.TweenScale = true; }
			}));
		}

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
			GetViewport()?.SetInputAsHandled();
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
