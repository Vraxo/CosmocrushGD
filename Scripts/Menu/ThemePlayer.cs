using Godot;
using System;

namespace CosmocrushGD;

public partial class ThemePlayer : AudioStreamPlayer
{
	private int currentThemeIndex = -1;
	private readonly Random random = new();
	private const string themePathTemplate = "res://Audio/Songs/CombatTheme/CombatTheme{0}.mp3";

	public override void _Ready()
	{
		PlayRandomTheme();
		Finished += OnThemeFinished;
	}

	public override void _ExitTree()
	{
		Finished -= OnThemeFinished;

		base._ExitTree();
	}

	private void PlayRandomTheme()
	{
		int nextThemeIndex;

		do
		{
			nextThemeIndex = random.Next(1, 7);
		}
		while (nextThemeIndex == currentThemeIndex);

		currentThemeIndex = nextThemeIndex;
		Stream = GD.Load<AudioStream>(string.Format(themePathTemplate, currentThemeIndex));
		Play();
	}

	private void OnThemeFinished()
	{
		PlayRandomTheme();
	}
}
