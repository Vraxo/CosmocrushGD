using Godot;

namespace CosmocrushGD;

public partial class ScoreDisplay : Label
{
    private StatisticsManager statisticsManagerInstance; // Store local reference

    public override void _Ready()
    {
        statisticsManagerInstance = StatisticsManager.Instance; // Get instance
        statisticsManagerInstance.EnsureLoaded(); // Ensure stats are loaded before accessing

        statisticsManagerInstance.ScoreChanged += UpdateScoreLabel;
        UpdateScoreLabel(statisticsManagerInstance.CurrentScore); // Initial update
    }

    public override void _ExitTree()
    {
        // Unsubscribe using the local reference if it's valid
        if (statisticsManagerInstance is not null)
        {
            statisticsManagerInstance.ScoreChanged -= UpdateScoreLabel;
        }
        // No need to check LazyInstance
    }

    private void UpdateScoreLabel(int newScore)
    {
        Text = $"Score: {newScore}";
    }
}