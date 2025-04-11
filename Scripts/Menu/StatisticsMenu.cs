using Godot;

namespace CosmocrushGD
{
	public partial class StatisticsMenu : ColorRect
	{
		[Export] private RichTextLabel gamesPlayedLabel;
		[Export] private RichTextLabel totalScoreLabel;
		[Export] private RichTextLabel topScoreLabel;
		[Export] private RichTextLabel averageScoreLabel;

		[Export] private Button clearButton;
		[Export] private Button returnButton;

		private const string LabelColor = "#AAAAAA";
		private const string ValueColor = "#FFFFFF";
		private const string TopScoreValueColor = "#FFFF00";

		private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

		public override void _Ready()
		{
			// Ensure nodes are valid before connecting signals
			if (clearButton != null && IsInstanceValid(clearButton))
			{
				clearButton.Pressed += OnClearButtonPressed;
			}
			else GD.PrintErr("StatisticsMenu: ClearButton node not found or invalid.");

			if (returnButton != null && IsInstanceValid(returnButton))
			{
				returnButton.Pressed += OnReturnButtonPressed;
			}
			else GD.PrintErr("StatisticsMenu: ReturnButton node not found or invalid.");


			// Ensure BBCode is enabled (can also be set in Inspector, this is belt-and-suspenders)
			EnableBbCode(gamesPlayedLabel);
			EnableBbCode(totalScoreLabel);
			EnableBbCode(topScoreLabel);
			EnableBbCode(averageScoreLabel);

			// Call LoadAndDisplayStats deferred to ensure layout is ready
			CallDeferred(nameof(LoadAndDisplayStats));
		}

		// Helper to reduce repetition
		private void EnableBbCode(RichTextLabel label)
		{
			if (label != null && IsInstanceValid(label))
			{
				label.BbcodeEnabled = true;
			}
			// Removed verbose logging
		}


		private void LoadAndDisplayStats()
		{
			// Ensure the manager is loaded if it hasn't been already
			var statsManager = StatisticsManager.Instance;
			if (statsManager == null)
			{
				GD.PrintErr("StatisticsMenu: StatisticsManager.Instance is null!");
				return;
			}
			var stats = statsManager.StatsData;
			if (stats == null)
			{
				GD.PrintErr("StatisticsMenu: StatisticsManager.Instance.StatsData is null!");
				return;
			}

			// Use BBCode to set text with different colors, include null checks
			SetLabelText(gamesPlayedLabel, $"[color={LabelColor}]Games Played:[/color] [color={ValueColor}]{stats.GamesPlayed}[/color]");
			SetLabelText(totalScoreLabel, $"[color={LabelColor}]Total Score:[/color] [color={ValueColor}]{stats.TotalScore}[/color]");
			SetLabelText(topScoreLabel, $"[color={LabelColor}]Top Score:[/color] [color={TopScoreValueColor}]{stats.TopScore}[/color]");
			SetLabelText(averageScoreLabel, $"[color={LabelColor}]Average Score:[/color] [color={ValueColor}]{statsManager.GetAverageScore():F2}[/color]");

			// Optional: Force a UI update if needed, though usually not required after deferred call
			// GetTree().Root.GuiEmbedSubwindows = !GetTree().Root.GuiEmbedSubwindows;
			// GetTree().Root.GuiEmbedSubwindows = !GetTree().Root.GuiEmbedSubwindows; // Hacky way, avoid if possible
			// UpdateMinimumSize(); // Might help containers recalculate
		}

		// Helper to set text
		private void SetLabelText(RichTextLabel label, string bbCodeText)
		{
			if (label != null && IsInstanceValid(label))
			{
				label.Text = bbCodeText;
				// Removed verbose logging
			}
			// Removed verbose logging
		}


		private void OnClearButtonPressed()
		{
			StatisticsManager.Instance.ResetStats();
			StatisticsManager.Instance.Save();
			LoadAndDisplayStats(); // Update labels immediately after clearing
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		}

		private void OnReturnButtonPressed()
		{
			GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
			GetTree().ChangeSceneToFile(MainMenuScenePath);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event.IsActionPressed("ui_cancel"))
			{
				OnReturnButtonPressed();
				GetViewport().SetInputAsHandled();
			}
		}

		public override void _ExitTree()
		{
			if (clearButton != null && IsInstanceValid(clearButton)) clearButton.Pressed -= OnClearButtonPressed;
			if (returnButton != null && IsInstanceValid(returnButton)) returnButton.Pressed -= OnReturnButtonPressed;
			base._ExitTree();
		}
	}
}
