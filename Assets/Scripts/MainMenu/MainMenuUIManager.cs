using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Handles the main-menu buttons and modal windows.</summary>
public class MainMenuUIManager : MonoBehaviour
{
    // These names match the serialized fields already present in MainMenu.unity.
    public GameObject SettingUI;
    public GameObject ExitUI;
    [SerializeField] private float sceneFadeDuration = 0.45f;

    private MainMenuBackgroundEffects _backgroundEffects;
    private bool _isStartingGame;

    private void Awake()
    {
        EnsureSettingsManager();
        if (SettingUI != null) SettingUI.SetActive(false);
        if (ExitUI != null) ExitUI.SetActive(false);
        SetupBackgroundEffects();
    }

    private void Update()
    {
        RefreshBackgroundEffectsPaused();
    }

    public void StartGame()
    {
        if (_isStartingGame)
            return;

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        _isStartingGame = true;
        SetMenuButtonsInteractable(false);

        yield return FadeToBlack();

        if (Application.CanStreamedLevelBeLoaded("InGameScene"))
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene");
        else
            Debug.LogError("InGameScene is not included in the build settings.", this);
    }

    public void SettingOn()
    {
        if (SettingUI != null) SettingUI.SetActive(true);
        RefreshBackgroundEffectsPaused();
    }

    public void QuitGame()
    {
        if (ExitUI != null) ExitUI.SetActive(true);
        RefreshBackgroundEffectsPaused();
    }

    public void QuitGameNo()
    {
        if (ExitUI != null) ExitUI.SetActive(false);
        RefreshBackgroundEffectsPaused();
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

    private void SetupBackgroundEffects()
    {
        _backgroundEffects = FindFirstObjectByType<MainMenuBackgroundEffects>();
        if (_backgroundEffects == null && Camera.main != null)
            _backgroundEffects = Camera.main.gameObject.AddComponent<MainMenuBackgroundEffects>();

        if (_backgroundEffects != null)
            _backgroundEffects.SetPauseTargets(SettingUI, ExitUI);
    }

    private void RefreshBackgroundEffectsPaused()
    {
        if (_backgroundEffects == null)
            return;

        var paused = (SettingUI != null && SettingUI.activeInHierarchy) ||
                     (ExitUI != null && ExitUI.activeInHierarchy);
        _backgroundEffects.SetPaused(paused);

        if (!paused && !_isStartingGame)
            RestoreMenuButtonsVisible();
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            button.interactable = interactable;
    }

    private void RestoreMenuButtonsVisible()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == "StartBtn" || button.name == "SettingBtn" || button.name == "QuitBtn")
                button.gameObject.SetActive(true);
        }
    }

    private IEnumerator FadeToBlack()
    {
        var fadeImage = CreateFadeImage();
        var duration = Mathf.Max(0.01f, sceneFadeDuration);

        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            var t = Mathf.Clamp01(elapsed / duration);
            var color = fadeImage.color;
            color.a = Mathf.SmoothStep(0f, 1f, t);
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.color = Color.black;
    }

    private static Image CreateFadeImage()
    {
        var canvasObject = new GameObject("Scene Transition Fade", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var imageObject = new GameObject("Black Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var image = imageObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }
}
