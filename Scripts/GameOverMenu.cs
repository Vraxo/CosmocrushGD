using Godot;

namespace CosmocrushGD;

public partial class GameOverMenu : ColorRect
{
    [Export] private Label scoreLabel;
    [Export] private Button playAgainButton;
    [Export] private Button returnButton;

    private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";
    private const string GameScenePath = "res://Scenes/World.tscn"; // Assumes this is your main game scene

    public override void _Ready()
    {
        if (playAgainButton is not null)
        {
            playAgainButton.Pressed += OnPlayAgainButtonPressed;
        }
        else
        {
            GD.PrintErr("GameOverMenu: Play Again Button not assigned!");
        }

        if (returnButton is not null)
        {
            returnButton.Pressed += OnReturnButtonPressed;
        }
        else
        {
            GD.PrintErr("GameOverMenu: Return Button not assigned!");
        }
    }

    public void SetScore(int score)
    {
        if (scoreLabel is not null)
        {
            scoreLabel.Text = $"Final Score: {score}";
        }
        else
        {
            GD.PrintErr("GameOverMenu: Score Label not assigned!");
        }
    }

    private void OnPlayAgainButtonPressed()
    {
        GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
        if (GetTree() is SceneTree tree)
        {
            tree.Paused = false;
            tree.ChangeSceneToFile(GameScenePath);
        }
    }

    private void OnReturnButtonPressed()
    {
        GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
        // Stats should have been saved in World.OnGameOver, but save again just in case.
        StatisticsManager.Instance.Save();

        if (GetTree() is SceneTree tree)
        {
            tree.Paused = false;
            tree.ChangeSceneToFile(MainMenuScenePath);
        }
    }

    public override void _ExitTree()
    {
        // Ensure game is unpaused if the menu is removed unexpectedly
        if (GetTree()?.Paused ?? false)
        {
            GetTree().Paused = false;
        }
    }
}