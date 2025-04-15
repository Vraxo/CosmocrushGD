using Godot;

namespace CosmocrushGD;

public partial class GameOverMenu : ColorRect
{
    [Export] private Label gameOverLabel;
    [Export] private Label scoreLabel;
    [Export] private Button playAgainButton;
    [Export] private Button returnButton;

    // Ensure this path points to the MenuShell scene
    private const string MainMenuScenePath = "res://Scenes/Menu/MenuShell.tscn";
    private const string GameScenePath = "res://Scenes/World.tscn";

    private const float FadeInDuration = 0.3f;
    private const float StaggerDelay = 0.15f;

    public override void _Ready()
    {
        if (gameOverLabel is null) GD.PrintErr("GameOverMenu: Game Over Label not assigned!");
        if (scoreLabel is null) GD.PrintErr("GameOverMenu: Score Label not assigned!");
        if (playAgainButton is null) GD.PrintErr("GameOverMenu: Play Again Button not assigned!");
        if (returnButton is null) GD.PrintErr("GameOverMenu: Return Button not assigned!");

        if (playAgainButton is not null)
        {
            playAgainButton.Pressed += OnPlayAgainButtonPressed;
        }
        if (returnButton is not null)
        {
            returnButton.Pressed += OnReturnButtonPressed;
        }

        SetInitialAlphas();
        CallDeferred(nameof(StartFadeInAnimation));
    }

    public void SetScore(int score)
    {
        if (scoreLabel is not null)
        {
            scoreLabel.Text = $"Final Score: {score}";
        }
    }

    private void SetInitialAlphas()
    {
        if (gameOverLabel is not null) gameOverLabel.Modulate = Colors.Transparent;
        if (scoreLabel is not null) scoreLabel.Modulate = Colors.Transparent;
        if (playAgainButton is not null) playAgainButton.Modulate = Colors.Transparent;
        if (returnButton is not null) returnButton.Modulate = Colors.Transparent;
    }

    private void StartFadeInAnimation()
    {
        Tween tween = CreateTween();
        tween.SetParallel(false);

        tween.TweenInterval(StaggerDelay);

        if (gameOverLabel is not null)
        {
            tween.TweenProperty(gameOverLabel, "modulate:a", 1.0f, FadeInDuration)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenInterval(StaggerDelay);
        }
        if (scoreLabel is not null)
        {
            tween.TweenProperty(scoreLabel, "modulate:a", 1.0f, FadeInDuration)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenInterval(StaggerDelay);
        }
        if (playAgainButton is not null)
        {
            tween.TweenProperty(playAgainButton, "modulate:a", 1.0f, FadeInDuration)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenInterval(StaggerDelay);
        }
        if (returnButton is not null)
        {
            tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.Play();
    }


    private void OnPlayAgainButtonPressed()
    {
        if (GetTree() is SceneTree tree)
        {
            tree.Paused = false;
            tree.ChangeSceneToFile(GameScenePath);
        }
    }

    private void OnReturnButtonPressed()
    {
        StatisticsManager.Instance.Save();

        if (GetTree() is SceneTree tree)
        {
            tree.Paused = false;
            // This line uses the MainMenuScenePath constant defined above
            tree.ChangeSceneToFile(MainMenuScenePath);
        }
    }

    public override void _ExitTree()
    {
        if (GetTree()?.Paused ?? false)
        {
            GetTree().Paused = false;
        }

        if (playAgainButton is not null && IsInstanceValid(playAgainButton))
        {
            playAgainButton.Pressed -= OnPlayAgainButtonPressed;
        }
        if (returnButton is not null && IsInstanceValid(returnButton))
        {
            returnButton.Pressed -= OnReturnButtonPressed;
        }
        base._ExitTree();
    }
}