using Godot;

namespace CosmocrushGD
{
	public partial class StatisticsMenu : ColorRect
	{
		[Export] private Label titleLabel;
		[Export] private RichTextLabel gamesPlayedLabel;
		[Export] private RichTextLabel totalScoreLabel;
		[Export] private RichTextLabel topScoreLabel;
		[Export] private RichTextLabel averageScoreLabel;
		[Export] private UIButton clearButton; // Changed type to UIButton
		[Export] private UIButton returnButton; // Changed type to UIButton
		[Export] private ConfirmationDialog confirmationDialog;

		private const string GamesPlayedDescColor = "#FFE000";
		private const string TotalScoreDescColor = "#87CEEB";
		private const string TopScoreDescColor = "#FFA07A";
		private const string AverageScoreDescColor = "#98FB98";
		private const string ValueColor = "#FFFFFF";

		private const float FadeInDuration = 0.3f;
		private const float StaggerDelay = 0.1f;
		private const float InitialScaleMultiplier = 2.0f;

		private MenuShell menuShell;

		public override void _Ready()
		{
			menuShell = GetParent()?.GetParent<MenuShell>();
			if (menuShell is null)
			{
				GD.PrintErr("StatisticsMenu: Could not find MenuShell parent!");
			}

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
				return;
			}

			clearButton.Pressed += OnClearButtonPressed;
			returnButton.Pressed += OnReturnButtonPressed;
			confirmationDialog.Confirmed += OnResetConfirmed;


			if (gamesPlayedLabel != null) gamesPlayedLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (totalScoreLabel != null) totalScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (topScoreLabel != null) topScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (averageScoreLabel != null) averageScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;

			LoadAndDisplayStats();

			CallDeferred(nameof(SetupPivots));
			SetInitialState();
			CallDeferred(nameof(StartFadeInAnimation));
		}

		private void SetupPivots()
		{
			// Important: GetSize() on RichTextLabel might not be reliable immediately
			// if FitContent is true. Ensure layout is settled. CallDeferred helps.
			if (titleLabel is not null) titleLabel.PivotOffset = titleLabel.Size / 2;
			if (gamesPlayedLabel is not null) gamesPlayedLabel.PivotOffset = gamesPlayedLabel.Size / 2;
			if (totalScoreLabel is not null) totalScoreLabel.PivotOffset = totalScoreLabel.Size / 2;
			if (topScoreLabel is not null) topScoreLabel.PivotOffset = topScoreLabel.Size / 2;
			if (averageScoreLabel is not null) averageScoreLabel.PivotOffset = averageScoreLabel.Size / 2;
			if (clearButton is not null) clearButton.PivotOffset = clearButton.Size / 2;
			if (returnButton is not null) returnButton.PivotOffset = returnButton.Size / 2;
		}

		private void SetInitialState()
		{
			Vector2 initialScale = Vector2.One;

			if (titleLabel is not null) { titleLabel.Modulate = Colors.Transparent; titleLabel.Scale = initialScale; }
			if (gamesPlayedLabel is not null) { gamesPlayedLabel.Modulate = Colors.Transparent; gamesPlayedLabel.Scale = initialScale; }
			if (totalScoreLabel is not null) { totalScoreLabel.Modulate = Colors.Transparent; totalScoreLabel.Scale = initialScale; }
			if (topScoreLabel is not null) { topScoreLabel.Modulate = Colors.Transparent; topScoreLabel.Scale = initialScale; }
			if (averageScoreLabel is not null) { averageScoreLabel.Modulate = Colors.Transparent; averageScoreLabel.Scale = initialScale; }
			if (clearButton is not null) { clearButton.Modulate = Colors.Transparent; clearButton.Scale = initialScale; clearButton.TweenScale = false; }
			if (returnButton is not null) { returnButton.Modulate = Colors.Transparent; returnButton.Scale = initialScale; returnButton.TweenScale = false; }
		}

		private void StartFadeInAnimation()
		{
			if (titleLabel is null || gamesPlayedLabel is null || totalScoreLabel is null ||
				topScoreLabel is null || averageScoreLabel is null || clearButton is null ||
				returnButton is null)
			{
				return;
			}

			// Ensure pivots are set before animating
			SetupPivots();

			Tween tween = CreateTween();
			tween.SetParallel(false);
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Back);

			Vector2 initialScaleValue = Vector2.One * InitialScaleMultiplier;
			Vector2 finalScale = Vector2.One;

			tween.TweenInterval(StaggerDelay); // Initial delay

			// Title
			if (titleLabel is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(titleLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenInterval(StaggerDelay);
			}

			// Games Played
			if (gamesPlayedLabel is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(gamesPlayedLabel, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(gamesPlayedLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenInterval(StaggerDelay);
			}

			// Total Score
			if (totalScoreLabel is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(totalScoreLabel, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(totalScoreLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenInterval(StaggerDelay);
			}

			// Top Score
			if (topScoreLabel is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(topScoreLabel, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(topScoreLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenInterval(StaggerDelay);
			}

			// Average Score
			if (averageScoreLabel is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(averageScoreLabel, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(averageScoreLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenInterval(StaggerDelay);
			}

			// Buttons
			if (clearButton is not null && returnButton is not null)
			{
				tween.SetParallel(true);
				tween.TweenProperty(clearButton, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(clearButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
				tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
				tween.SetParallel(false);
				tween.TweenCallback(Callable.From(() =>
				{
					if (clearButton is not null) { clearButton.TweenScale = true; }
					if (returnButton is not null) { returnButton.TweenScale = true; }
				}));
			}

			tween.Play();
		}

		private void LoadAndDisplayStats()
		{
			var statsManager = StatisticsManager.Instance;
			var stats = statsManager.StatsData;

			// Using PushColor/Pop prevents BBCode parsing issues if values contain special chars
			if (gamesPlayedLabel != null)
			{
				gamesPlayedLabel.Clear();
				gamesPlayedLabel.PushColor(Color.FromHtml(GamesPlayedDescColor));
				gamesPlayedLabel.AppendText("Games Played: ");
				gamesPlayedLabel.Pop();
				gamesPlayedLabel.PushColor(Color.FromHtml(ValueColor));
				gamesPlayedLabel.AppendText($"{stats.GamesPlayed}");
				gamesPlayedLabel.Pop();
			}
			if (totalScoreLabel != null)
			{
				totalScoreLabel.Clear();
				totalScoreLabel.PushColor(Color.FromHtml(TotalScoreDescColor));
				totalScoreLabel.AppendText("Total Score: ");
				totalScoreLabel.Pop();
				totalScoreLabel.PushColor(Color.FromHtml(ValueColor));
				totalScoreLabel.AppendText($"{stats.TotalScore}");
				totalScoreLabel.Pop();
			}
			if (topScoreLabel != null)
			{
				topScoreLabel.Clear();
				topScoreLabel.PushColor(Color.FromHtml(TopScoreDescColor));
				topScoreLabel.AppendText("Top Score: ");
				topScoreLabel.Pop();
				topScoreLabel.PushColor(Color.FromHtml(ValueColor));
				topScoreLabel.AppendText($"{stats.TopScore}");
				topScoreLabel.Pop();
			}
			if (averageScoreLabel != null)
			{
				averageScoreLabel.Clear();
				averageScoreLabel.PushColor(Color.FromHtml(AverageScoreDescColor));
				averageScoreLabel.AppendText("Average Score: ");
				averageScoreLabel.Pop();
				averageScoreLabel.PushColor(Color.FromHtml(ValueColor));
				averageScoreLabel.AppendText($"{statsManager.GetAverageScore():F2}");
				averageScoreLabel.Pop();
			}
		}


		private void OnClearButtonPressed()
		{
			if (confirmationDialog != null)
			{
				confirmationDialog.PopupCentered();
			}
			else
			{
				GD.PrintErr("Cannot show confirmation dialog because confirmationDialog reference is NULL!");
			}
		}

		private void OnResetConfirmed()
		{
			StatisticsManager.Instance.ResetStats();
			StatisticsManager.Instance.Save();
			LoadAndDisplayStats(); // Reload to show the reset values
								   // Optional: Re-run fade-in animation for the labels? Might be jarring.
		}


		private void OnReturnButtonPressed()
		{
			menuShell?.ShowMainMenu();
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
				GetViewport()?.SetInputAsHandled();
			}
		}

		public override void _ExitTree()
		{
			if (clearButton != null && IsInstanceValid(clearButton))
			{
				clearButton.Pressed -= OnClearButtonPressed;
			}
			if (returnButton != null && IsInstanceValid(returnButton))
			{
				returnButton.Pressed -= OnReturnButtonPressed;
			}
			if (confirmationDialog != null && IsInstanceValid(confirmationDialog))
			{
				confirmationDialog.Confirmed -= OnResetConfirmed;
			}

			base._ExitTree();
		}
	}
}
