using Godot;
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CosmocrushGD;

public sealed class StatisticsManager
{
	private static readonly Lazy<StatisticsManager> LazyInstance = new(() => new StatisticsManager());
	public static StatisticsManager Instance => LazyInstance.Value;

	public StatisticsData Stats { get; private set; }

	public int CurrentScore { get; private set; } = 0;

	public event Action<int> ScoreChanged;
	public event Action StatisticsUpdated; // Event for when persistent stats change

	private const string StatisticsFilePath = "user://Statistics.yaml";
	private bool isLoaded = false;

	private StatisticsManager()
	{
		// Loading is deferred until first access or explicit call
	}

	public void EnsureLoaded()
	{
		if (!isLoaded)
		{
			Load();
		}
	}

	public void Load()
	{
		if (isLoaded)
		{
			return;
		}

		if (!FileAccess.FileExists(StatisticsFilePath))
		{
			Stats = new();
			isLoaded = true;
			Save(); // Create the file with defaults if it doesn't exist
			return;
		}

		try
		{
			using var file = FileAccess.Open(StatisticsFilePath, FileAccess.ModeFlags.Read);
			string yamlString = file.GetAsText();

			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.Build();

			Stats = deserializer.Deserialize<StatisticsData>(yamlString) ?? new();
		}
		catch (Exception exception) when (exception is YamlException || exception is System.IO.IOException)
		{
			GD.PrintErr($"Error loading statistics: {exception.Message}. Resetting statistics.");
			Stats = new();
			Save(); // Try to save default stats if loading failed
		}

		isLoaded = true;
		// Reset current score on load, as it's session-specific
		CurrentScore = 0;
		ScoreChanged?.Invoke(CurrentScore);
		StatisticsUpdated?.Invoke();
	}

	public void Save()
	{
		EnsureLoaded(); // Make sure we have data to save

		try
		{
			var serializer = new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults) // Use OmitDefaults for cleaner YAML
				.Build();

			string yamlString = serializer.Serialize(Stats);

			using var file = FileAccess.Open(StatisticsFilePath, FileAccess.ModeFlags.Write);
			file.StoreString(yamlString);
		}
		catch (System.IO.IOException exception)
		{
			GD.PrintErr($"Error saving statistics: {exception.Message}");
		}
	}

	public void IncrementScore(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		CurrentScore += amount;
		ScoreChanged?.Invoke(CurrentScore);
	}

	public void EndGame()
	{
		EnsureLoaded();

		Stats.GamesPlayed++;
		Stats.TotalScore += CurrentScore;

		if (CurrentScore > Stats.TopScore)
		{
			Stats.TopScore = CurrentScore;
		}

		Save(); // Save updated stats
		StatisticsUpdated?.Invoke();

		// Reset current score for the next game
		CurrentScore = 0;
		ScoreChanged?.Invoke(CurrentScore);
	}

	public double GetAverageScore()
	{
		EnsureLoaded();

		if (Stats.GamesPlayed == 0)
		{
			return 0.0;
		}

		return (double)Stats.TotalScore / Stats.GamesPlayed;
	}
}