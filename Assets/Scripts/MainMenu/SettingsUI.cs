using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

/// <summary>Edits settings in a temporary copy until Apply is clicked.</summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Audio")] public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    [Header("Graphics")] public TMP_Dropdown resolutionDropdown;
    public Toggle fullScreenToggle;
    public TMP_Dropdown qualityDropdown;
    [Header("Buttons")] public UIButton applyButton;
    public UIButton cancelButton;
    public UIButton resetButton;

    private GameSettings tempSettings;

    private void OnEnable()
    {
        if (SettingManager.Instance == null) return;
        tempSettings = SettingManager.Instance.currentSettings.Clone();
        InitUI();
    }

    private void OnDisable() => RemoveListeners();

    private void InitUI()
    {
        RemoveListeners();
        var manager = SettingManager.Instance;
        if (manager == null || tempSettings == null) return;

        SetSlider(masterSlider, tempSettings.masterVolume);
        SetSlider(bgmSlider, tempSettings.bgmVolume);
        SetSlider(sfxSlider, tempSettings.sfxVolume);
        if (fullScreenToggle != null) fullScreenToggle.SetIsOnWithoutNotify(tempSettings.fullScreen);

        if (resolutionDropdown != null)
        {
            var resolutions = manager.GetResolutions();
            var options = new List<string>();
            foreach (var resolution in resolutions) options.Add($"{resolution.width} x {resolution.height}");
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(tempSettings.resolutionIndex, 0, Mathf.Max(0, resolutions.Length - 1)));
            resolutionDropdown.RefreshShownValue();
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(tempSettings.qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1)));
            qualityDropdown.RefreshShownValue();
        }
        RegisterListeners();
    }

    private void RegisterListeners()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(value => tempSettings.masterVolume = value);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(value => tempSettings.bgmVolume = value);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(value => tempSettings.sfxVolume = value);
        if (fullScreenToggle != null) fullScreenToggle.onValueChanged.AddListener(value => tempSettings.fullScreen = value);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(value => tempSettings.resolutionIndex = value);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(value => tempSettings.qualityLevel = value);
        if (applyButton != null) applyButton.onClick.AddListener(OnApply);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
    }

    private void RemoveListeners()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveAllListeners();
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (fullScreenToggle != null) fullScreenToggle.onValueChanged.RemoveAllListeners();
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveAllListeners();
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveAllListeners();
        // Preserve any callbacks configured in the Inspector; only remove this component's handlers.
        if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
        if (resetButton != null) resetButton.onClick.RemoveListener(OnReset);
    }

    private void OnApply()
    {
        if (SettingManager.Instance == null || tempSettings == null) return;
        SettingManager.Instance.currentSettings = tempSettings.Clone();
        SettingManager.Instance.ApplySettings();
        gameObject.SetActive(false);
    }

    private void OnCancel() => gameObject.SetActive(false);
    private void OnReset()
    {
        tempSettings = new GameSettings { resolutionIndex = SettingManager.Instance.GetDefaultResolutionIndex() };
        InitUI();
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider != null) slider.SetValueWithoutNotify(value);
    }
}
