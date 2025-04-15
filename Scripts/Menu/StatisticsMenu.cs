using Godot;

namespace CosmocrushGD
{
	public partial class StatisticsMenu : ColorRect
	{
		[Export] private Label titleLabel; // Added export
		[Export] private RichTextLabel gamesPlayedLabel;
		[Export] private RichTextLabel totalScoreLabel;
		[Export] private RichTextLabel topScoreLabel;
		[Export] private RichTextLabel averageScoreLabel;
		[Export] private Button clearButton;
		[Export] private Button returnButton;
		[Export] private ConfirmationDialog confirmationDialog;

		private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

		private const string GamesPlayedDescColor = "#FFE000";
		private const string TotalScoreDescColor = "#87CEEB";
		private const string TopScoreDescColor = "#FFA07A";
		private const string AverageScoreDescColor = "#98FB98";
		private const string ValueColor = "#FFFFFF";

		private const float FadeInDuration = 0.15f;
		private const float StaggerDelay = 0.075f;

		public override void _Ready()
		{
			bool initializationFailed = false;
			if (titleLabel == null) { GD.PrintErr("StatisticsMenu: titleLabel is NULL!"); initializationFailed = true; }
			if (gamesPlayedLabel == null) { GD.PrintErr("StatisticsMenu: gamesPlayedLabel is NULL!"); initializationFailed = true; }
			if (totalScoreLabel == null) { GD.PrintErr("StatisticsMenu: totalScoreLabel is NULL!"); initializationFailed = true; }
			if (topScoreLabel == null) { GD.PrintErr("StatisticsMenu: topScoreLabel is NULL!"); initializationFailed = true; }
			if (averageScoreLabel == null) { GD.PrintErr("StatisticsMenu: averageScoreLabel is NULL!"); initializationFailed = true; }
			if (clearButton == null) { GD.PrintErr("StatisticsMenu: clearButton is NULL!"); initializationFailed = true; }
			if (returnButton == null) { GD.PrintErr("StatisticsMenu: returnButton is NULL!"); initializationFailed = true; }
			if (confirmationDialog == null) { GD.PrintErr("StatisticsMenu: confirmationDialog is NULL!"); initializationFailed = true; }

			if (initializationFailed)
			{
				GD.PrintErr("StatisticsMenu: Initialization failed due to missing UI node references.");
				return; // Stop execution if nodes are missing
			}

			if (!clearButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnClearButtonPressed)))
			{
				clearButton.Pressed += OnClearButtonPressed;
			}

			if (!returnButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnReturnButtonPressed)))
			{
				returnButton.Pressed += OnReturnButtonPressed;
			}

			if (!confirmationDialog.IsConnected(ConfirmationDialog.SignalName.Confirmed, Callable.From(OnResetConfirmed)))
			{
				confirmationDialog.Confirmed += OnResetConfirmed;
			}


			if (gamesPlayedLabel != null) gamesPlayedLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (totalScoreLabel != null) totalScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (topScoreLabel != null) topScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (averageScoreLabel != null) averageScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;

			LoadAndDisplayStats();

			SetInitialAlphas();
			CallDeferred(nameof(StartFadeInAnimation));
		}

		private void SetInitialAlphas()
		{
			if (titleLabel is not null) titleLabel.Modulate = Colors.Transparent;
			if (gamesPlayedLabel is not null) gamesPlayedLabel.Modulate = Colors.Transparent;
			if (totalScoreLabel is not null) totalScoreLabel.Modulate = Colors.Transparent;
			if (topScoreLabel is not null) topScoreLabel.Modulate = Colors.Transparent;
			if (averageScoreLabel is not null) averageScoreLabel.Modulate = Colors.Transparent;
			if (clearButton is not null) clearButton.Modulate = Colors.Transparent;
			if (returnButton is not null) returnButton.Modulate = Colors.Transparent;
		}

		private void StartFadeInAnimation()
		{
			// Extra check before starting animation
			if (titleLabel is null || gamesPlayedLabel is null || totalScoreLabel is null ||
				topScoreLabel is null || averageScoreLabel is null || clearButton is null ||
				returnButton is null)
			{
				GD.PrintErr("StatisticsMenu: Cannot start animation, one or more UI nodes are null.");
				return;
			}

			Tween tween = CreateTween();
			tween.SetParallel(false);

			tween.TweenInterval(StaggerDelay);

			// Animate Title
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);

			// Animate Stat Labels sequentially
			tween.TweenProperty(gamesPlayedLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);

			tween.TweenProperty(totalScoreLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);

			tween.TweenProperty(topScoreLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);

			tween.TweenProperty(averageScoreLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);

			// Animate Buttons together
			tween.SetParallel(true);
			tween.TweenProperty(clearButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);

			tween.Play();
		}

		private void LoadAndDisplayStats()
		{
			var statsManager = StatisticsManager.Instance;
			var stats = statsManager.StatsData;

			if (gamesPlayedLabel != null)
				gamesPlayedLabel.Text = $"[color={GamesPlayedDescColor}]Games Played:[/color] [color={ValueColor}]{stats.GamesPlayed}[/color]";
			if (totalScoreLabel != null)
				totalScoreLabel.Text = $"[color={TotalScoreDescColor}]Total Score:[/color] [color={ValueColor}]{stats.TotalScore}[/color]";
			if (topScoreLabel != null)
				topScoreLabel.Text = $"[color={TopScoreDescColor}]Top Score:[/color] [color={ValueColor}]{stats.TopScore}[/color]";
			if (averageScoreLabel != null)
				averageScoreLabel.Text = $"[color={AverageScoreDescColor}]Average Score:[/color] [color={ValueColor}]{statsManager.GetAverageScore():F2}[/color]";
		}

		private void OnClearButtonPressed()
		{
			GD.Print("Clear Stats button pressed.");
			if (confirmationDialog != null)
			{
				confirmationDialog.PopupCentered();
			}
			else
			{
				GD.PrintErr("Cannot show confirmation dialog because confirmationDialog reference is NULL!");
			}
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		}

		private void OnResetConfirmed()
		{
			GD.Print("Reset Confirmed.");
			StatisticsManager.Instance.ResetStats();
			StatisticsManager.Instance.Save();
			LoadAndDisplayStats();
		}


		private void OnReturnButtonPressed()
		{
			GD.Print("Return button pressed.");
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
			GetTree().ChangeSceneToFile(MainMenuScenePath);
		}

		public override void _Input(InputEvent @event)
		{
			if (confirmationDialog != null && confirmationDialog.Visible)
			{
				return;
			}

			if (@event.IsActionPressed("ui_cancel"))
			{
				OnReturnButtonPressed();
				GetViewport().SetInputAsHandled();
			}
		}

		public override void _ExitTree()
		{
			if (clearButton != null && IsInstanceValid(clearButton))
			{
				if (clearButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnClearButtonPressed)))
				{
					clearButton.Pressed -= OnClearButtonPressed;
				}
			}
			if (returnButton != null && IsInstanceValid(returnButton))
			{
				if (returnButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnReturnButtonPressed)))
				{
					returnButton.Pressed -= OnReturnButtonPressed;
				}
			}

			if (confirmationDialog != null && IsInstanceValid(confirmationDialog))
			{
				if (confirmationDialog.IsConnected(ConfirmationDialog.SignalName.Confirmed, Callable.From(OnResetConfirmed)))
				{
					confirmationDialog.Disconnect(ConfirmationDialog.SignalName.Confirmed, Callable.From(OnResetConfirmed));
				}
			}
			base._ExitTree();
		}
	}
}
