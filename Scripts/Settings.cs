using Godot;
using System.Text.Json;

public sealed class Settings
{
    private static Settings? _instance;
    public static Settings Instance => _instance ??= new();

    public SettingsData SettingsData;
    private const string SettingsFilePath = "user://Settings.json";

    public void Load()
    {
        FileAccess file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Read);

        if (FileAccess.GetOpenError() == Error.FileNotFound)
        {
            SettingsData = new()
            {
                MasterVolume = 1.0,
                MusicVolume = 1.0,
                SfxVolume = 1.0
            };

            return;
        }

        string jsonString = file.GetAsText();
        file.Close();

        try
        {
            SettingsData = JsonSerializer.Deserialize<SettingsData>(jsonString);
            SettingsData.MasterVolume = Mathf.Clamp(SettingsData.MasterVolume, 0.0, 1.0);
            SettingsData.MusicVolume = Mathf.Clamp(SettingsData.MusicVolume, 0.0, 1.0);
            SettingsData.SfxVolume = Mathf.Clamp(SettingsData.SfxVolume, 0.0, 1.0);
        }
        catch (JsonException e)
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
        var jsonString = JsonSerializer.Serialize(SettingsData, new JsonSerializerOptions { WriteIndented = true });
        FileAccess file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Write);
        file.StoreString(jsonString);
        file.Close();

        UpdateAudioLevels();
    }

    private void UpdateAudioLevels()
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)Mathf.LinearToDb(SettingsData.MasterVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), (float)Mathf.LinearToDb(SettingsData.MusicVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), (float)Mathf.LinearToDb(SettingsData.SfxVolume));
    }
}