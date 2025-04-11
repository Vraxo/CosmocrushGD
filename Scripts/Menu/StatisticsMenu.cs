using Godot;

namespace CosmocrushGD
{
    public partial class StatisticsMenu : ColorRect
    {
        [Export] private Label gamesPlayedLabel;
        [Export] private Label totalScoreLabel;
        [Export] private Label topScoreLabel;
        [Export] private Label averageScoreLabel;
        [Export] private Button clearButton;
        [Export] private Button returnButton;

        private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

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

            LoadAndDisplayStats();
        }

        private void LoadAndDisplayStats()
        {
            // Ensure the manager is loaded if it hasn't been already
            var statsManager = StatisticsManager.Instance;
            var stats = statsManager.StatsData;

            if (gamesPlayedLabel != null)
                gamesPlayedLabel.Text = $"Games Played: {stats.GamesPlayed}";
            if (totalScoreLabel != null)
                totalScoreLabel.Text = $"Total Score: {stats.TotalScore}";
            if (topScoreLabel != null)
                topScoreLabel.Text = $"Top Score: {stats.TopScore}";
            if (averageScoreLabel != null)
                averageScoreLabel.Text = $"Average Score: {statsManager.GetAverageScore():F2}"; // Format to 2 decimal places
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