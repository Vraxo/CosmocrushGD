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

		private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

		// Define colors for BBCode
		private const string DescriptionColor = "#CCCCCC"; // Light Grey
		private const string ValueColor = "#FFFFFF";       // White

		public override void _Ready()
		{
			if (clearButton != null)
			{
				clearButton.Pressed += OnClearButtonPressed;
			}
			if (returnButton != null)
			{
				returnButton.Pressed += OnReturnButtonPressed;
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

			// Use BBCode to set colors. This works directly with RichTextLabel's Text property.
			if (gamesPlayedLabel != null)
				gamesPlayedLabel.Text = $"[color={DescriptionColor}]Games Played:[/color] [color={ValueColor}]{stats.GamesPlayed}[/color]";
			if (totalScoreLabel != null)
				totalScoreLabel.Text = $"[color={DescriptionColor}]Total Score:[/color] [color={ValueColor}]{stats.TotalScore}[/color]";
			if (topScoreLabel != null)
				topScoreLabel.Text = $"[color={DescriptionColor}]Top Score:[/color] [color={ValueColor}]{stats.TopScore}[/color]";
			if (averageScoreLabel != null)
				averageScoreLabel.Text = $"[color={DescriptionColor}]Average Score:[/color] [color={ValueColor}]{statsManager.GetAverageScore():F2}[/color]"; // Format to 2 decimal places
		}

		private void OnClearButtonPressed()
		{
			GD.Print("Clear Stats button pressed.");
			// Optional: Add a confirmation dialog here

			StatisticsManager.Instance.ResetStats(); // Reset the data
			StatisticsManager.Instance.Save();      // Save the reset state immediately
			LoadAndDisplayStats();                  // Update the labels on screen
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound); // Play feedback sound
		}

		private void OnReturnButtonPressed()
		{
			GD.Print("Return button pressed.");
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound); // Play feedback sound
			GetTree().ChangeSceneToFile(MainMenuScenePath);
		}

		public override void _Input(InputEvent @event)
		{
			// Allow escaping back to the main menu
			if (@event.IsActionPressed("ui_cancel"))
			{
				OnReturnButtonPressed();
				GetViewport().SetInputAsHandled();
			}
		}

		public override void _ExitTree()
		{
			if (IsInstanceValid(clearButton)) clearButton.Pressed -= OnClearButtonPressed;
			if (IsInstanceValid(returnButton)) returnButton.Pressed -= OnReturnButtonPressed;
			base._ExitTree();
		}
	}
}
