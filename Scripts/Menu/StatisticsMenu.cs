using Godot;

namespace CosmocrushGD
{
	public partial class StatisticsMenu : ColorRect
	{
		// Change type from Label to RichTextLabel
		[Export] private RichTextLabel gamesPlayedLabel;
		[Export] private RichTextLabel totalScoreLabel;
		[Export] private RichTextLabel topScoreLabel;
		[Export] private RichTextLabel averageScoreLabel;
		[Export] private Button clearButton;
		[Export] private Button returnButton;
		[Export] private ConfirmationDialog confirmationDialog; // Export the dialog

		private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

		// --- Define colors for BBCode ---
		// Unique colors for the descriptive part of the text
		private const string GamesPlayedDescColor = "#FFE000";     // Even Less Saturated Yellow (Changed from #FFEB00)
		private const string TotalScoreDescColor = "#87CEEB";      // Sky Blue (different shade)
		private const string TopScoreDescColor = "#FFA07A";        // Light Salmon
		private const string AverageScoreDescColor = "#98FB98";    // Pale Green

		// Consistent color for the value part of the text
		private const string ValueColor = "#FFFFFF";       // White
		// --- End Color Definitions ---

		public override void _Ready()
		{
			// --- Debugging Checks ---
			if (clearButton == null)
				GD.PrintErr("StatisticsMenu: clearButton is NULL in _Ready!");
			if (returnButton == null)
				GD.PrintErr("StatisticsMenu: returnButton is NULL in _Ready!");
			if (confirmationDialog == null)
				GD.PrintErr("StatisticsMenu: confirmationDialog is NULL in _Ready!");
			// --- End Debugging Checks ---

			if (clearButton != null)
			{
				// Defensive check: Ensure not already connected if scene reloads weirdly
				if (!clearButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnClearButtonPressed)))
				{
					clearButton.Pressed += OnClearButtonPressed;
				}
			}
			if (returnButton != null)
			{
				if (!returnButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnReturnButtonPressed)))
				{
					returnButton.Pressed += OnReturnButtonPressed;
				}
			}
			// Connect the dialog's confirmed signal
			if (confirmationDialog != null)
			{
				if (!confirmationDialog.IsConnected(ConfirmationDialog.SignalName.Confirmed, Callable.From(OnResetConfirmed)))
				{
					confirmationDialog.Confirmed += OnResetConfirmed;
				}
			}


			// No longer need to explicitly enable BBCode, RichTextLabel does it by default.
			// Set AutowrapMode if needed (usually good to keep off for single lines)
			if (gamesPlayedLabel != null) gamesPlayedLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (totalScoreLabel != null) totalScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (topScoreLabel != null) topScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			if (averageScoreLabel != null) averageScoreLabel.AutowrapMode = TextServer.AutowrapMode.Off;

			LoadAndDisplayStats();
		}

		private void LoadAndDisplayStats()
		{
			// Ensure the manager is loaded if it hasn't been already
			var statsManager = StatisticsManager.Instance;
			var stats = statsManager.StatsData;

			// Use BBCode to set specific colors for each description, and white for the value
			if (gamesPlayedLabel != null)
				gamesPlayedLabel.Text = $"[color={GamesPlayedDescColor}]Games Played:[/color] [color={ValueColor}]{stats.GamesPlayed}[/color]";
			if (totalScoreLabel != null)
				totalScoreLabel.Text = $"[color={TotalScoreDescColor}]Total Score:[/color] [color={ValueColor}]{stats.TotalScore}[/color]";
			if (topScoreLabel != null)
				topScoreLabel.Text = $"[color={TopScoreDescColor}]Top Score:[/color] [color={ValueColor}]{stats.TopScore}[/color]";
			if (averageScoreLabel != null)
				averageScoreLabel.Text = $"[color={AverageScoreDescColor}]Average Score:[/color] [color={ValueColor}]{statsManager.GetAverageScore():F2}[/color]"; // Format to 2 decimal places
		}

		private void OnClearButtonPressed()
		{
			GD.Print("Clear Stats button pressed.");
			// Show the confirmation dialog instead of resetting directly
			if (confirmationDialog != null)
			{
				confirmationDialog.PopupCentered();
			}
			else
			{
				GD.PrintErr("Cannot show confirmation dialog because confirmationDialog reference is NULL!");
			}
			// Play UI sound when initiating the action
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		}

		// This method is called when the user clicks "OK" on the ConfirmationDialog
		private void OnResetConfirmed()
		{
			GD.Print("Reset Confirmed.");
			StatisticsManager.Instance.ResetStats(); // Reset the data
			StatisticsManager.Instance.Save();      // Save the reset state immediately
			LoadAndDisplayStats();                  // Update the labels on screen
		}


		private void OnReturnButtonPressed()
		{
			GD.Print("Return button pressed.");
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound); // Play feedback sound
			GetTree().ChangeSceneToFile(MainMenuScenePath);
		}

		public override void _Input(InputEvent @event)
		{
			// Allow escaping back to the main menu, but not if the dialog is visible
			if (confirmationDialog != null && confirmationDialog.Visible)
			{
				return; // Don't allow escape if dialog is open
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

			// Disconnect dialog signal
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
