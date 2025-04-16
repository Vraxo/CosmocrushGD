using Godot;

namespace CosmocrushGD;

public partial class GameOverMenu : ColorRect
{
    [Export] private Label gameOverLabel;
    [Export] private Label scoreLabel;
    [Export] private UIButton playAgainButton; // Changed type
    [Export] private UIButton returnButton; // Changed type

    private const string MainMenuScenePath = "res://Scenes/Menu/MenuShell.tscn";
    private const string GameScenePath = "res://Scenes/World.tscn";

    private const float FadeInDuration = 0.3f;
    private const float StaggerDelay = 0.1f;
    private const float InitialScaleMultiplier = 2.0f;
    private const float ScoreCountDuration = 0.8f; // Duration for score animation

    private int _targetScore = 0;
    private float _animatedScoreValue = 0f; // Use float for tweening

    // Property to tween, updates label in setter
    private float AnimatedScoreValue
    {
        get => _animatedScoreValue;
        set
        {
            _animatedScoreValue = value;
            UpdateScoreLabelText();
        }
    }


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

        if (scoreLabel is not null)
        {
            scoreLabel.Text = "Final Score: 0";
        }

        CallDeferred(nameof(SetupPivots));
        SetInitialState();
    }

    private void SetupPivots()
    {
        if (gameOverLabel is not null) gameOverLabel.PivotOffset = gameOverLabel.Size / 2;
        if (scoreLabel is not null) scoreLabel.PivotOffset = scoreLabel.Size / 2;
        if (playAgainButton is not null) playAgainButton.PivotOffset = playAgainButton.Size / 2;
        if (returnButton is not null) returnButton.PivotOffset = returnButton.Size / 2;
    }

    private void SetInitialState()
    {
        Vector2 initialScale = Vector2.One;

        if (gameOverLabel is not null) { gameOverLabel.Modulate = Colors.Transparent; gameOverLabel.Scale = initialScale; }
        if (scoreLabel is not null) { scoreLabel.Modulate = Colors.Transparent; scoreLabel.Scale = initialScale; }
        if (playAgainButton is not null) { playAgainButton.Modulate = Colors.Transparent; playAgainButton.Scale = initialScale; playAgainButton.TweenScale = false; }
        if (returnButton is not null) { returnButton.Modulate = Colors.Transparent; returnButton.Scale = initialScale; returnButton.TweenScale = false; }
    }

    public void SetScore(int score)
    {
        _targetScore = score;
        _animatedScoreValue = 0f;

        if (scoreLabel is not null)
        {
            scoreLabel.Text = "Final Score: 0";
        }

        CallDeferred(nameof(SetupPivots));
        CallDeferred(nameof(StartFadeInAnimation));
    }

    private void UpdateScoreLabelText()
    {
        if (scoreLabel is not null)
        {
            scoreLabel.Text = $"Final Score: {Mathf.RoundToInt(AnimatedScoreValue)}";
        }
    }

    private void StartFadeInAnimation()
    {
        if (gameOverLabel is null || scoreLabel is null || playAgainButton is null || returnButton is null)
        {
            GD.PrintErr("GameOverMenu: Cannot start animation, one or more nodes are null.");
            return;
        }

        SetupPivots();

        Tween tween = CreateTween();
        tween.SetParallel(false);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);

        Vector2 initialScaleValue = Vector2.One * InitialScaleMultiplier;
        Vector2 finalScale = Vector2.One;

        tween.TweenInterval(StaggerDelay);

        if (gameOverLabel is not null)
        {
            tween.SetParallel(true);
            tween.TweenProperty(gameOverLabel, "modulate:a", 1.0f, FadeInDuration);
            tween.TweenProperty(gameOverLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
            tween.SetParallel(false);
            tween.TweenInterval(StaggerDelay);
        }

        if (scoreLabel is not null)
        {
            tween.SetParallel(true);
            tween.TweenProperty(scoreLabel, "modulate:a", 1.0f, FadeInDuration);
            tween.TweenProperty(scoreLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
            tween.SetParallel(false);
        }

        // *** Corrected Line Here ***
        tween.TweenProperty(this, "AnimatedScoreValue", _targetScore, ScoreCountDuration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);

        tween.TweenInterval(ScoreCountDuration * 0.5);

        if (playAgainButton is not null)
        {
            tween.SetParallel(true);
            tween.TweenProperty(playAgainButton, "modulate:a", 1.0f, FadeInDuration);
            tween.TweenProperty(playAgainButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
            tween.SetParallel(false);
            tween.TweenInterval(StaggerDelay);
        }

        if (returnButton is not null)
        {
            tween.SetParallel(true);
            tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
            tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
            tween.SetParallel(false);
            tween.TweenCallback(Callable.From(() =>
            {
                if (playAgainButton is not null) { playAgainButton.TweenScale = true; }
                if (returnButton is not null) { returnButton.TweenScale = true; }
            }));
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