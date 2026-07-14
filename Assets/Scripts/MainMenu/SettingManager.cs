using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public enum VolumeType { MasterVolume, BGMVolume, SFXVolume }

/// <summary>Persists and applies the settings shared by every scene.</summary>
public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    public AudioMixer audioMixer;
    public GameSettings currentSettings;

    private Resolution[] resolutions;
    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, "settings.json");
        resolutions = Screen.resolutions;
        LoadSettings();

        if (currentSettings.resolutionIndex < 0)
            currentSettings.resolutionIndex = GetDefaultResolutionIndex();

        ApplySettings();
    }

    public int GetDefaultResolutionIndex()
    {
        if (resolutions == null || resolutions.Length == 0) return 0;

        var current = Screen.currentResolution;
        float targetRatio = current.height == 0 ? 16f / 9f : (float)current.width / current.height;
        int bestIndex = 0;
        int bestPixels = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            var resolution = resolutions[i];
            float ratio = (float)resolution.width / resolution.height;
            int pixels = resolution.width * resolution.height;
            if (Mathf.Abs(ratio - targetRatio) < 0.03f && pixels > bestPixels)
            {
                bestIndex = i;
                bestPixels = pixels;
            }
        }
        return bestIndex;
    }

    public void ApplySettings()
    {
        if (currentSettings == null) currentSettings = new GameSettings();
        currentSettings.masterVolume = Mathf.Clamp01(currentSettings.masterVolume);
        currentSettings.bgmVolume = Mathf.Clamp01(currentSettings.bgmVolume);
        currentSettings.sfxVolume = Mathf.Clamp01(currentSettings.sfxVolume);
        ApplyAudio();
        ApplyGraphics();
        SaveSettings();
    }

    public void ApplyAudio()
    {
        SetMixerVolume(VolumeType.MasterVolume.ToString(), currentSettings.masterVolume);
        SetMixerVolume(VolumeType.BGMVolume.ToString(), currentSettings.bgmVolume);
        SetMixerVolume(VolumeType.SFXVolume.ToString(), currentSettings.sfxVolume);
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        if (audioMixer == null) return;
        float decibels = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        audioMixer.SetFloat(parameterName, decibels);
    }

    public void ApplyGraphics()
    {
        if (resolutions != null && resolutions.Length > 0)
        {
            currentSettings.resolutionIndex = Mathf.Clamp(currentSettings.resolutionIndex, 0, resolutions.Length - 1);
            var resolution = resolutions[currentSettings.resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, currentSettings.fullScreen);
        }

        if (QualitySettings.names.Length > 0)
        {
            currentSettings.qualityLevel = Mathf.Clamp(currentSettings.qualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        }
    }

    public Resolution[] GetResolutions() => resolutions ?? System.Array.Empty<Resolution>();

    public void SaveSettings()
    {
        try { File.WriteAllText(savePath, JsonUtility.ToJson(currentSettings, true)); }
        catch (System.Exception exception) { Debug.LogError($"Failed to save settings: {exception.Message}"); }
    }

    public void LoadSettings()
    {
        try
        {
            currentSettings = File.Exists(savePath)
                ? JsonUtility.FromJson<GameSettings>(File.ReadAllText(savePath))
                : new GameSettings();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Settings file could not be loaded; using defaults. {exception.Message}");
            currentSettings = new GameSettings();
        }

        currentSettings ??= new GameSettings();
    }
}
