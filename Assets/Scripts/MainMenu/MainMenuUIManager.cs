using UnityEngine;

/// <summary>Handles the main-menu buttons and modal windows.</summary>
public class MainMenuUIManager : MonoBehaviour
{
    // These names match the serialized fields already present in MainMenu.unity.
    public GameObject SettingUI;
    public GameObject ExitUI;

    private void Awake()
    {
        EnsureSettingsManager();
        if (SettingUI != null) SettingUI.SetActive(false);
        if (ExitUI != null) ExitUI.SetActive(false);
    }

    public void StartGame()
    {
        if (Application.CanStreamedLevelBeLoaded("InGameScene"))
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene");
        else
            Debug.LogError("InGameScene is not included in the build settings.", this);
    }

    public void SettingOn()
    {
        if (SettingUI != null) SettingUI.SetActive(true);
    }

    public void QuitGame()
    {
        if (ExitUI != null) ExitUI.SetActive(true);
    }

    public void QuitGameNo()
    {
        if (ExitUI != null) ExitUI.SetActive(false);
    }

    public void QuitGameYes()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void EnsureSettingsManager()
    {
        if (SettingManager.Instance != null) return;
        new GameObject("SettingManager").AddComponent<SettingManager>();
    }
}
