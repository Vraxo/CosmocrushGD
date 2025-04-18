using Godot;
using System;
using System.Threading.Tasks; // Required for Task

namespace CosmocrushGD;

public partial class TransitionScreen : CanvasLayer
{
	[Signal]
	public delegate void TransitionMidpointReachedEventHandler(string scenePathToLoad);
	[Signal]
	public delegate void TransitionFinishedEventHandler();

	[Export] private ColorRect colorRect;
	[Export] private AnimationPlayer animationPlayer;

	public static TransitionScreen Instance { get; private set; }

	private string _targetScenePath = string.Empty;
	private bool _isTransitioning = false; // Prevent overlapping transitions

	private const string FadeToBlackAnimationName = "FadeToBlack";
	private const string FadeToNormalAnimationName = "FadeToNormal";
	private const float PostFadeDelay = 0.05f; // Small delay (in seconds)

	public override void _EnterTree()
	{
		if (Instance is not null)
		{
			GD.PrintErr($"TransitionScreen: Duplicate instance detected. Removing new one: {Name}");
			QueueFree();
			return;
		}
		Instance = this;
		GD.Print($"TransitionScreen: Singleton instance assigned: {Name}");
	}

	public override void _Ready()
	{
		if (animationPlayer is not null)
		{
			animationPlayer.AnimationFinished += OnAnimationFinished;
		}
		else
		{
			GD.PrintErr($"TransitionScreen ({Name}): AnimationPlayer is not assigned!");
		}

		if (colorRect is null)
		{
			GD.PrintErr($"TransitionScreen ({Name}): ColorRect is not assigned!");
		}
		else
		{
			// Start invisible
			colorRect.Modulate = Colors.Transparent; // Fully transparent
			colorRect.Visible = false;
		}
	}

	public void TransitionToScene(string scenePath)
	{
		if (_isTransitioning)
		{
			GD.Print("TransitionScreen: Already transitioning, ignoring new request.");
			return;
		}
		if (string.IsNullOrEmpty(scenePath))
		{
			GD.PrintErr("TransitionScreen: TransitionToScene called with null or empty scene path.");
			return;
		}

		if (colorRect is null || animationPlayer is null)
		{
			GD.PrintErr("TransitionScreen: Cannot transition, nodes not ready. Changing scene directly.");
			GetTree().ChangeSceneToFile(scenePath);
			return;
		}

		_isTransitioning = true;
		_targetScenePath = scenePath;
		colorRect.Modulate = Colors.Transparent; // Ensure start at transparent
		colorRect.Visible = true;
		GD.Print($"TransitionScreen: Playing '{FadeToBlackAnimationName}' for scene: {scenePath}. Current time: {Time.GetTicksMsec()}");
		animationPlayer.Play(FadeToBlackAnimationName);
	}

	public void StartFadeIn()
	{
		// Don't start fade-in if we are mid-fade-out
		if (_isTransitioning && animationPlayer?.CurrentAnimation == FadeToBlackAnimationName)
		{
			GD.Print("TransitionScreen: Cannot StartFadeIn during FadeToBlack.");
			return;
		}

		if (colorRect is null || animationPlayer is null)
		{
			GD.PrintErr("TransitionScreen: Cannot StartFadeIn, nodes not ready.");
			return;
		}

		_isTransitioning = true; // Mark as transitioning (fade in)
		colorRect.Modulate = Colors.Black; // Ensure start at black
		colorRect.Visible = true;
		GD.Print($"TransitionScreen: Playing '{FadeToNormalAnimationName}'. Current time: {Time.GetTicksMsec()}");
		animationPlayer.Play(FadeToNormalAnimationName);
	}

	// Make the handler async to allow await
	private async void OnAnimationFinished(StringName animationName)
	{
		GD.Print($"TransitionScreen: Animation finished: {animationName}. Current time: {Time.GetTicksMsec()}");
		if (animationName == FadeToBlackAnimationName)
		{
			if (!string.IsNullOrEmpty(_targetScenePath))
			{
				// Add a small delay to ensure the final frame renders
				await Task.Delay((int)(PostFadeDelay * 1000));
				// Alternative Godot timer await:
				// await ToSignal(GetTree().CreateTimer(PostFadeDelay), Timer.SignalName.Timeout);

				GD.Print($"TransitionScreen: Post-delay, Emitting TransitionMidpointReached for {_targetScenePath}. Current time: {Time.GetTicksMsec()}");
				EmitSignal(SignalName.TransitionMidpointReached, _targetScenePath);
				_targetScenePath = string.Empty; // Clear path after emitting
												 // _isTransitioning should remain true until FadeToNormal finishes in the new scene
			}
			else
			{
				GD.PushWarning("TransitionScreen: FadeToBlack finished but target scene path was empty.");
				_isTransitioning = false; // Reset if something went wrong
			}
		}
		else if (animationName == FadeToNormalAnimationName)
		{
			if (colorRect is not null)
			{
				colorRect.Visible = false; // Hide after fading in
			}
			_isTransitioning = false; // Transition fully complete
			GD.Print("TransitionScreen: Emitting TransitionFinished.");
			EmitSignal(SignalName.TransitionFinished);
		}
	}

	public override void _ExitTree()
	{
		if (animationPlayer is not null && IsInstanceValid(animationPlayer))
		{
			if (animationPlayer.IsConnected(AnimationPlayer.SignalName.AnimationFinished, Callable.From<StringName>(OnAnimationFinished)))
			{
				animationPlayer.AnimationFinished -= OnAnimationFinished;
			}
		}

		if (Instance == this)
		{
			Instance = null;
			GD.Print("TransitionScreen: Singleton instance cleared.");
		}
		base._ExitTree();
	}
}
