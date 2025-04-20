using Godot;

namespace CosmocrushGD;

public partial class StatisticsMenu : ColorRect
{
	[Export] private Label titleLabel;
	[Export] private RichTextLabel gamesPlayedLabel;
	[Export] private RichTextLabel totalScoreLabel;
	[Export] private RichTextLabel topScoreLabel;
	[Export] private RichTextLabel averageScoreLabel;
	[Export] private UIButton clearButton;
	[Export] private UIButton returnButton;
	[Export] private ConfirmationDialog confirmationDialog;

	private MenuShell menuShell;

	private const string GamesPlayedDescriptionColor = "#FFE000";
	private const string TotalScoreDescriptionColor = "#87CEEB";
	private const string TopScoreDescriptionColor = "#FFA07A";
	private const string AverageScoreDescriptionColor = "#98FB98";
	private const string ValueColor = "#FFFFFF";
	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.1f;
	private const float InitialScaleMultiplier = 2.0f;

	public override void _Ready()
	{
		menuShell = GetParent()?.GetParent()?.GetParent<MenuShell>();
		if (menuShell is null)
		{
			GD.PrintErr("StatisticsMenu: Could not find MenuShell parent!");
		}

		if (titleLabel is null ||
			gamesPlayedLabel is null ||
			totalScoreLabel is null ||
			topScoreLabel is null ||
			averageScoreLabel is null ||
			clearButton is null ||
			returnButton is null ||
			confirmationDialog is null)
		{
			GD.PrintErr("StatisticsMenu: One or more required node references are missing in the inspector! Aborting setup.");
			return;
		}

		clearButton.Pressed += OnClearButtonPressed;
		returnButton.Pressed += OnReturnButtonPressed;
		confirmationDialog.Confirmed += OnResetConfirmed;

		gamesPlayedLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		totalScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		topScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		averageScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;

		LoadAndDisplayStats();

		CallDeferred(nameof(SetupPivots));
		SetInitialState();
		CallDeferred(nameof(StartFadeInAnimation));
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (confirmationDialog is not null && confirmationDialog.Visible)
		{
			return;
		}

		if (inputEvent.IsActionPressed("ui_cancel"))
		{
			OnReturnButtonPressed();
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _ExitTree()
	{
			clearButton.Pressed -= OnClearButtonPressed;
			returnButton.Pressed -= OnReturnButtonPressed;
			confirmationDialog.Confirmed -= OnResetConfirmed;

		base._ExitTree();
	}

	private void SetupPivots()
	{
		titleLabel.PivotOffset = titleLabel.Size / 2;
		gamesPlayedLabel.PivotOffset = gamesPlayedLabel.Size / 2;
		totalScoreLabel.PivotOffset = totalScoreLabel.Size / 2;
		topScoreLabel.PivotOffset = topScoreLabel.Size / 2;
		averageScoreLabel.PivotOffset = averageScoreLabel.Size / 2;
		clearButton.PivotOffset = clearButton.Size / 2;
		returnButton.PivotOffset = returnButton.Size / 2;
	}

	private void SetInitialState()
	{
		SetControlInitialState(titleLabel);
		SetControlInitialState(gamesPlayedLabel);
		SetControlInitialState(totalScoreLabel);
		SetControlInitialState(topScoreLabel);
		SetControlInitialState(averageScoreLabel);
		SetControlInitialState(clearButton);
		SetControlInitialState(returnButton);
	}

	private void StartFadeInAnimation()
	{
		if (titleLabel is null || gamesPlayedLabel is null || totalScoreLabel is null ||
			topScoreLabel is null || averageScoreLabel is null || clearButton is null ||
			returnButton is null)
		{
			GD.PrintErr("StatisticsMenu: Cannot start fade-in animation, one or more controls are null.");
			return;
		}

		SetupPivots();

		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		var initialScaleValue = Vector2.One * InitialScaleMultiplier;
		var finalScale = Vector2.One;

		tween.TweenInterval(StaggerDelay);

		AddFadeInTween(tween, titleLabel, finalScale, initialScaleValue);
		AddFadeInTween(tween, gamesPlayedLabel, finalScale, initialScaleValue);
		AddFadeInTween(tween, totalScoreLabel, finalScale, initialScaleValue);
		AddFadeInTween(tween, topScoreLabel, finalScale, initialScaleValue);
		AddFadeInTween(tween, averageScoreLabel, finalScale, initialScaleValue);

		tween.SetParallel(true);
		tween.TweenProperty(clearButton, "modulate:a", 1.0f, FadeInDuration);
		tween.TweenProperty(clearButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
		tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
		tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(() =>
		{
			clearButton.TweenScale = true;
			returnButton.TweenScale = true;
		}));


		tween.Play();
	}

	private void LoadAndDisplayStats()
	{
		var statsManager = StatisticsManager.Instance;
		StatisticsData stats = statsManager.StatsData;

		FormatStatLabel(gamesPlayedLabel, "Games Played: ", GamesPlayedDescriptionColor, $"{stats.GamesPlayed}");
		FormatStatLabel(totalScoreLabel, "Total Score: ", TotalScoreDescriptionColor, $"{stats.TotalScore}");
		FormatStatLabel(topScoreLabel, "Top Score: ", TopScoreDescriptionColor, $"{stats.TopScore}");
		FormatStatLabel(averageScoreLabel, "Average Score: ", AverageScoreDescriptionColor, $"{statsManager.GetAverageScore():F2}");
	}

	private void OnClearButtonPressed()
	{
		if (confirmationDialog is not null)
		{
			confirmationDialog.PopupCentered();
		}
		else
		{
			GD.PrintErr("StatisticsMenu: Confirmation Dialog is null!");
		}
	}

	private void OnResetConfirmed()
	{
		StatisticsManager.Instance.ResetStats();
		StatisticsManager.Instance.Save();
		LoadAndDisplayStats();
	}

	private void OnReturnButtonPressed()
	{
		menuShell?.ShowMainMenu();
	}

	private static void SetControlInitialState(Control control)
	{
		if (control is null)
		{
			return;
		}

		control.Modulate = Colors.Transparent;
		control.Scale = Vector2.One;

		if (control is UIButton uiButton)
		{
			uiButton.TweenScale = false;
		}
	}

	private static void AddFadeInTween(Tween tween, Control control, Vector2 finalScale, Vector2 initialScale)
	{
		if (control is null)
		{
			return;
		}

		tween.SetParallel(true);
		tween.TweenProperty(control, Control.PropertyName.Modulate.ToString() + ":a", 1.0f, FadeInDuration);
		tween.TweenProperty(control, Control.PropertyName.Scale.ToString(), finalScale, FadeInDuration).From(initialScale);
		tween.SetParallel(false);
		tween.TweenInterval(StaggerDelay);
	}

	private static void FormatStatLabel(RichTextLabel label, string description, string descriptionColor, string value)
	{
		if (label is null)
		{
			return;
		}

		label.Clear();
		label.PushColor(Color.FromHtml(descriptionColor));
		label.AppendText(description);
		label.Pop();
		label.PushColor(Color.FromHtml(ValueColor));
		label.AppendText(value);
		label.Pop();
	}
}
