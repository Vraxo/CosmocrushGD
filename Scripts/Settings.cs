using Godot;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CosmocrushGD;

public sealed class Settings
{
    private static Settings? _instance;
    public static Settings Instance => _instance ??= new();

    public SettingsData SettingsData;
    private const string SettingsFilePath = "user://Settings.yaml";

    public void Load()
    {
        FileAccess file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Read);

        if (FileAccess.GetOpenError() == Error.FileNotFound)
        {
            SettingsData = new SettingsData
            {
                MasterVolume = 1.0,
                MusicVolume = 1.0,
                SfxVolume = 1.0
            };
            return;
        }

        string yamlString = file.GetAsText();
        file.Close();

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            SettingsData = deserializer.Deserialize<SettingsData>(yamlString);

            SettingsData.MasterVolume = Mathf.Clamp(SettingsData.MasterVolume, 0.0, 1.0);
            SettingsData.MusicVolume = Mathf.Clamp(SettingsData.MusicVolume, 0.0, 1.0);
            SettingsData.SfxVolume = Mathf.Clamp(SettingsData.SfxVolume, 0.0, 1.0);
        }
        catch (YamlException e)
        {
            GD.PrintErr($"Error loading settings: {e.Message}");

            SettingsData = new()
            {
                MasterVolume = 1.0,
                MusicVolume = 1.0,
                SfxVolume = 1.0
            };
        }

        UpdateAudioLevels();
    }

    public void Save()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        string yamlString = serializer.Serialize(SettingsData);

        FileAccess file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Write);
        file.StoreString(yamlString);
        file.Close();

        UpdateAudioLevels();
    }

    private void UpdateAudioLevels()
    {
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Master"),
            (float)Mathf.LinearToDb(SettingsData.MasterVolume));

        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Music"),
            (float)Mathf.LinearToDb(SettingsData.MusicVolume));

        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("SFX"),
            (float)Mathf.LinearToDb(SettingsData.SfxVolume));
    }
}