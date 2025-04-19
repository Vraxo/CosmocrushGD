using Godot;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CosmocrushGD;

public sealed class StatisticsManager
{
    private static StatisticsManager _instance;
    public static StatisticsManager Instance => _instance ??= new StatisticsManager();

    public StatisticsData StatsData { get; private set; }
    private const string StatsFilePath = "user://Statistics.yaml";
    private bool _dirty = false;

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
        _dirty = false;
    }

    public void Save()
    {
        if (!_dirty)
        {
            return;
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
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
        _dirty = false;
        GD.Print($"Saved statistics: Played={StatsData.GamesPlayed}, Total={StatsData.TotalScore}, Top={StatsData.TopScore}");
    }

    public void IncrementGamesPlayed()
    {
        StatsData.GamesPlayed++;
        _dirty = true;
        GD.Print($"Incremented Games Played to: {StatsData.GamesPlayed}");
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
    }

    public double GetAverageScore()
    {
        if (StatsData.GamesPlayed == 0)
        {
            return 0.0;
        }
        return (double)StatsData.TotalScore / StatsData.GamesPlayed;
    }

    public void ResetStats()
    {
        StatsData = new StatisticsData();
        _dirty = true;
        GD.Print("Statistics have been reset.");
    }
}