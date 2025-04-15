using Godot;

namespace CosmocrushGD;

public partial class SettingsMenu : ColorRect
{
	// References to interactive elements
	[Export] private Label titleLabel;
	[Export] private HSlider musicSlider;
	[Export] private HSlider sfxSlider;
	[Export] private Label musicLabel;
	[Export] private Label sfxLabel;
	[Export] private Button applyButton;
	[Export] private Button returnButton;

	// References to containers/layout nodes to fade
	private Control titleContainer; // Contains TitleLabel
	private HBoxContainer musicHBox; // Contains MusicLabel and MusicSlider
	private HBoxContainer sfxHBox;   // Contains SfxLabel and SfxSlider
	private HBoxContainer buttonHBox;// Contains ApplyButton and ReturnButton

	private float previousMusicVolume;
	private float previousSfxVolume;

	private const float FadeInDuration = 0.35f; // Slightly longer duration
	private const float StaggerDelay = 0.15f; // Slightly longer delay
	private readonly Vector2 InitialScale = new(1.3f, 1.3f);
	private readonly Vector2 TargetScale = Vector2.One;

	private MenuShell menuShell;

	public override void _Ready()
	{
		menuShell = GetParent()?.GetParent<MenuShell>();
		if (menuShell is null)
		{
			GD.PrintErr("SettingsMenu: Could not find MenuShell parent!");
		}

		bool initializationFailed = false;

		// Get interactive elements
		titleLabel ??= GetNode<Label>("CenterContainer/VBoxContainer/TitleContainer/TitleLabel");
		musicSlider ??= GetNode<HSlider>("CenterContainer/VBoxContainer/MusicContainer/MusicHBox/Slider");
		sfxSlider ??= GetNode<HSlider>("CenterContainer/VBoxContainer/SfxContainer/SfxHBox/Slider");
		musicLabel ??= GetNode<Label>("CenterContainer/VBoxContainer/MusicContainer/MusicHBox/MusicLabel");
		sfxLabel ??= GetNode<Label>("CenterContainer/VBoxContainer/SfxContainer/SfxHBox/SfxLabel");
		applyButton ??= GetNode<Button>("CenterContainer/VBoxContainer/ButtonContainer/ButtonHBox/ApplyButton");
		returnButton ??= GetNode<Button>("CenterContainer/VBoxContainer/ButtonContainer/ButtonHBox/ReturnButton");

		// Get container elements to fade
		// Note: We get the HBoxContainers for rows with sliders, not their parent Controls
		titleContainer ??= GetNode<Control>("CenterContainer/VBoxContainer/TitleContainer");
		musicHBox ??= GetNode<HBoxContainer>("CenterContainer/VBoxContainer/MusicContainer/MusicHBox");
		sfxHBox ??= GetNode<HBoxContainer>("CenterContainer/VBoxContainer/SfxContainer/SfxHBox");
		buttonHBox ??= GetNode<HBoxContainer>("CenterContainer/VBoxContainer/ButtonContainer/ButtonHBox");


		// Check all references
		if (titleLabel is null || titleContainer is null) { GD.PrintErr("SettingsMenu: Title elements missing!"); initializationFailed = true; }
		if (musicSlider is null || musicLabel is null || musicHBox is null) { GD.PrintErr("SettingsMenu: Music elements missing!"); initializationFailed = true; }
		if (sfxSlider is null || sfxLabel is null || sfxHBox is null) { GD.PrintErr("SettingsMenu: SFX elements missing!"); initializationFailed = true; }
		if (applyButton is null || returnButton is null || buttonHBox is null) { GD.PrintErr("SettingsMenu: Button elements missing!"); initializationFailed = true; }


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

		SetInitialVisuals();
		CallDeferred(nameof(StartFadeInAnimation)); // Defer animation start
		UpdateApplyAvailability();
	}

	private void SetInitialVisuals()
	{
		// Make containers transparent initially
		SetContainerInitialAlpha(titleContainer);
		SetContainerInitialAlpha(musicHBox);
		SetContainerInitialAlpha(sfxHBox);
		SetContainerInitialAlpha(buttonHBox);

		// Set initial scale for elements that WILL be scaled
		SetElementInitialScale(titleLabel);
		SetElementInitialScale(musicLabel);
		// --- Do NOT scale musicSlider ---
		SetElementInitialScale(sfxLabel);
		// --- Do NOT scale sfxSlider ---
		SetElementInitialScale(applyButton);
		SetElementInitialScale(returnButton);

		// Ensure sliders start at normal scale and visible (alpha handled by parent HBox)
		if (musicSlider is not null) musicSlider.Scale = TargetScale;
		if (sfxSlider is not null) sfxSlider.Scale = TargetScale;

	}

	// Sets container transparent
	private void SetContainerInitialAlpha(Control container)
	{
		if (container is not null)
		{
			container.Modulate = Colors.Transparent;
		}
	}

	// Sets scale for elements that need it
	private void SetElementInitialScale(Control element)
	{
		if (element is not null)
		{
			element.Scale = InitialScale;
		}
	}


	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false); // Overall sequence is sequential
		tween.SetEase(Tween.EaseType.Out); // Easing for fade

		tween.TweenInterval(StaggerDelay); // Initial delay

		// Animate Title Row
		AnimateRow(tween, titleContainer, new Control[] { titleLabel });

		// Animate Music Row
		AnimateRow(tween, musicHBox, new Control[] { musicLabel }); // Only scale the label

		// Animate SFX Row
		AnimateRow(tween, sfxHBox, new Control[] { sfxLabel }); // Only scale the label

		// Animate Button Row
		AnimateRow(tween, buttonHBox, new Control[] { applyButton, returnButton }); // Scale both buttons

		tween.Play();
	}

	// Animates one row: Fades the container, scales specific elements within
	private void AnimateRow(Tween tween, Control containerToFade, Control[] elementsToScale)
	{
		if (containerToFade is null)
		{
			return;
		}

		// Start parallel animation for this row
		tween.SetParallel(true);

		// 1. Animate Container Alpha (Fade In)
		tween.TweenProperty(containerToFade, "modulate:a", 1.0f, FadeInDuration);

		// 2. Animate Scale for specific elements within the container
		foreach (var element in elementsToScale)
		{
			if (element is not null)
			{
				// *** Set PivotOffset here, just before tweening scale ***
				element.PivotOffset = element.Size / 2.0f;

				tween.TweenProperty(element, "scale", TargetScale, FadeInDuration)
					 .SetTrans(Tween.TransitionType.Back); // Use Back transition for scale effect
			}
		}

		// End parallel animation for this row
		tween.SetParallel(false);

		// Add delay *after* this row's animation for the next one
		tween.TweenInterval(StaggerDelay);
	}


	// --- Rest of the methods remain the same ---
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
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
	}

	private void OnReturnButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
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
