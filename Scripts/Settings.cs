﻿using Godot;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CosmocrushGD;

public sealed class Settings
{
    private static Settings _instance;
    public static Settings Instance => _instance ??= new();

    public SettingsData SettingsData;
    private const string SettingsFilePath = "user://Settings.yaml";

    private Settings()
    {
        Load();
    }

    public void Load()
    {
        FileAccess file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Read);

        if (FileAccess.GetOpenError() == Error.FileNotFound)
        {
            SettingsData = new()
            {
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

            SettingsData.MusicVolume = Mathf.Clamp(SettingsData.MusicVolume, 0.0, 1.0);
            SettingsData.SfxVolume = Mathf.Clamp(SettingsData.SfxVolume, 0.0, 1.0);
        }
        catch (YamlException e)
        {
            GD.PrintErr($"Error loading settings: {e.Message}");

            SettingsData = new()
            {
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
        UpdateAudioLevel("Music", SettingsData.MusicVolume);
        UpdateAudioLevel("SFX", SettingsData.SfxVolume);
    }

    private static void UpdateAudioLevel(string busName, double volume)
    {
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex(busName),
            (float)Mathf.LinearToDb(volume));
    }
}
