using Godot;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CosmocrushGD
{
	public sealed class StatisticsManager
	{
		private static StatisticsManager _instance;
		public static StatisticsManager Instance => _instance ??= new StatisticsManager();

		public StatisticsData StatsData { get; private set; }
		private const string StatsFilePath = "user://Statistics.yaml";
		private bool _dirty = false; // Flag to track if data needs saving

		private StatisticsManager()
		{
			Load();
		}

		public void Load()
		{
			FileAccess file = FileAccess.Open(StatsFilePath, FileAccess.ModeFlags.Read);

			if (FileAccess.GetOpenError() == Error.FileNotFound)
			{
				StatsData = new StatisticsData();
				GD.Print("Statistics file not found. Created new stats data.");
				// Don't mark as dirty, nothing to save yet.
				return;
			}

			string yamlString = file.GetAsText();
			file.Close();

			try
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();

				StatsData = deserializer.Deserialize<StatisticsData>(yamlString) ?? new StatisticsData();
				GD.Print($"Loaded statistics: Played={StatsData.GamesPlayed}, Total={StatsData.TotalScore}, Top={StatsData.TopScore}");
			}
			catch (YamlException e)
			{
				GD.PrintErr($"Error loading statistics: {e.Message}. Resetting stats.");
				StatsData = new StatisticsData();
			}
			_dirty = false; // Loaded successfully, no need to save immediately.
		}

		public void Save()
		{
			// Only save if there are changes
			if (!_dirty)
			{
				// GD.Print("StatisticsManager: No changes to save.");
				return;
			}

			var serializer = new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull) // Or OmitDefaults
				.Build();

			string yamlString;
			try
			{
				yamlString = serializer.Serialize(StatsData);
			}
			catch (YamlException e)
			{
				GD.PrintErr($"Error serializing statistics: {e.Message}");
				return;
			}


			FileAccess file = FileAccess.Open(StatsFilePath, FileAccess.ModeFlags.Write);
			if (FileAccess.GetOpenError() != Error.Ok)
			{
				GD.PrintErr($"Failed to open statistics file for writing: {FileAccess.GetOpenError()}");
				return;
			}
			file.StoreString(yamlString);
			file.Close();
			_dirty = false; // Data is now saved
			GD.Print($"Saved statistics: Played={StatsData.GamesPlayed}, Total={StatsData.TotalScore}, Top={StatsData.TopScore}");
		}

		public void IncrementGamesPlayed()
		{
			StatsData.GamesPlayed++;
			_dirty = true;
			GD.Print($"Incremented Games Played to: {StatsData.GamesPlayed}");
			// Consider saving immediately or batching saves
			// Save(); // Uncomment if you want to save every time a game starts
		}

		public void UpdateScores(int currentScore)
		{
			StatsData.TotalScore += currentScore;
			if (currentScore > StatsData.TopScore)
			{
				StatsData.TopScore = currentScore;
				GD.Print($"New Top Score: {StatsData.TopScore}");
			}
			_dirty = true;
			GD.Print($"Updated Scores: Total={StatsData.TotalScore}, Current={currentScore}");
			// Save immediately after a game ends seems reasonable
			Save();
		}

		public double GetAverageScore()
		{
			if (StatsData.GamesPlayed == 0)
			{
				return 0.0;
			}
			// Ensure floating point division
			return (double)StatsData.TotalScore / StatsData.GamesPlayed;
		}

		// New method to reset statistics
		public void ResetStats()
		{
			StatsData = new StatisticsData(); // Create a new blank data object
			_dirty = true; // Mark as dirty so it gets saved
			GD.Print("Statistics have been reset.");
			// Save(); // Call Save here if you want the reset to persist immediately
			// It's called explicitly in StatisticsMenu after calling ResetStats now.
		}
	}
}