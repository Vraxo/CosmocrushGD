using Godot;
using System;
using System.IO;

namespace CosmocrushGD;

public partial class ThemePlayer : AudioStreamPlayer
{
	private Random random = new();
	private int currentThemeIndex = -1;
	private string themePathTemplate = "res://Audio/Songs/CombatTheme/CombatTheme{0}.mp3";

	public override void _Ready()
	{
		// PlayRandomTheme(); // Don't play immediately
		CallDeferred(nameof(PlayRandomTheme)); // Play after a short delay
		Finished += OnThemeFinished;
	}

	public override void _ExitTree()
	{
		// Ensure signal is disconnected if the node is freed
		if (IsConnected(AudioStreamPlayer.SignalName.Finished, Callable.From(OnThemeFinished)))
		{
			Finished -= OnThemeFinished;
		}
		base._ExitTree();
	}

	private void PlayRandomTheme()
	{
		if (!IsInsideTree()) return; // Don't try to play if node was removed before deferred call

		int nextThemeIndex;
		do
		{
			nextThemeIndex = random.Next(1, 7); // Assuming 6 themes
		}
		while (nextThemeIndex == currentThemeIndex && 6 > 1); // Avoid infinite loop if only 1 theme exists

		currentThemeIndex = nextThemeIndex;
		Stream = GD.Load<AudioStream>(string.Format(themePathTemplate, currentThemeIndex));
		Play();
	}

	private void OnThemeFinished()
	{
		PlayRandomTheme();
	}
}
