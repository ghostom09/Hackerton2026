using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

public class ResultUIManager : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private SpeedRunTImer speedRunTimer;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text clearText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text newRecordText;
    [SerializeField] private UIButton retryButton;
    [SerializeField] private UIButton titleButton;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "MainMenu";

    [Header("Behaviour")]
    [SerializeField] private bool buildUIOnAwake = true;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool pauseGameOnShow = true;
    [SerializeField] private bool stopSpeedRunTimerOnShow = true;
    [SerializeField] private float newRecordPopScale = 1.16f;
    [SerializeField] private float newRecordPopDuration = 0.22f;

    public UnityEvent RetryClicked;
    public UnityEvent TitleClicked;

    private Coroutine _newRecordRoutine;
    private Vector3 _newRecordBaseScale = Vector3.one;

    private void Awake()
    {
        if (speedRunTimer == null)
            speedRunTimer = FindFirstObjectByType<SpeedRunTImer>();

        if (buildUIOnAwake)
            EnsureUI();

        if (newRecordText != null)
            _newRecordBaseScale = newRecordText.rectTransform.localScale;

        BindButtons();

        if (hideOnAwake && canvas != null)
            canvas.gameObject.SetActive(false);
    }

    public void ShowResult(float clearTime, float bestTime, string rank, bool isNewRecord)
    {
        EnsureUI();

        if (stopSpeedRunTimerOnShow && speedRunTimer != null)
            speedRunTimer.StopTimer();

        if (pauseGameOnShow)
            Time.timeScale = 0f;

        if (canvas != null)
            canvas.gameObject.SetActive(true);

        if (timeText != null)
            timeText.text = $"Time  {FormatTime(clearTime)}";

        if (bestTimeText != null)
            bestTimeText.text = $"Best Time  {(bestTime > 0f ? FormatTime(bestTime) : "--:--.--")}";

        if (rankText != null)
            rankText.text = $"Rank  {(string.IsNullOrWhiteSpace(rank) ? "D" : rank)}";

        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(isNewRecord);
            if (isNewRecord)
            {
                if (_newRecordRoutine != null)
                    StopCoroutine(_newRecordRoutine);

                _newRecordRoutine = StartCoroutine(PlayNewRecordPop());
            }
        }
    }

    public void ShowResultFromSpeedRunTimer(float bestTime, string rank, bool isNewRecord)
    {
        var clearTime = speedRunTimer != null ? speedRunTimer.ElapsedTime : 0f;
        ShowResult(clearTime, bestTime, rank, isNewRecord);
    }

    public void HideResult()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        RetryClicked?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        TitleClicked?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }

    private void EnsureUI()
    {
        if (canvas != null && backgroundOverlay != null && contentRoot != null && clearText != null &&
            timeText != null && bestTimeText != null && rankText != null && retryButton != null && titleButton != null)
            return;

        if (canvas == null)
            canvas = CreateCanvas();

        if (backgroundOverlay == null)
            backgroundOverlay = CreateOverlay(canvas.transform);

        if (contentRoot == null)
            contentRoot = CreateContentRoot(canvas.transform);

        if (clearText == null)
            clearText = CreateText("Clear Text", contentRoot, "CLEAR!", 74f, FontStyles.Bold, new Color(1f, 1f, 1f), new Vector2(0f, 178f), new Vector2(520f, 100f));

        if (newRecordText == null)
        {
            newRecordText = CreateText("New Record Text", contentRoot, "NEW RECORD!", 30f, FontStyles.Bold, new Color(1f, 0.78f, 0.18f), new Vector2(0f, 108f), new Vector2(520f, 54f));
            newRecordText.gameObject.SetActive(false);
            _newRecordBaseScale = newRecordText.rectTransform.localScale;
        }

        if (timeText == null)
            timeText = CreateStatText("Time Text", contentRoot, "Time", "--:--.--", 38f, 36f, 38f);

        if (bestTimeText == null)
            bestTimeText = CreateStatText("Best Time Text", contentRoot, "Best Time", "--:--.--", 38f, -8f, 30f);

        if (rankText == null)
            rankText = CreateStatText("Rank Text", contentRoot, "Rank", "D", 38f, -52f, 34f);

        if (retryButton == null)
            retryButton = CreateButton("Retry Button", contentRoot, "Retry", new Vector2(0f, -132f));

        if (titleButton == null)
            titleButton = CreateButton("Title Button", contentRoot, "Title", new Vector2(0f, -194f));
    }

    private void BindButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(Retry);
            retryButton.onClick.AddListener(Retry);
        }

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(GoToTitle);
            titleButton.onClick.AddListener(GoToTitle);
        }
    }

    private IEnumerator PlayNewRecordPop()
    {
        var rect = newRecordText.rectTransform;
        var duration = Mathf.Max(0.01f, newRecordPopDuration);
        rect.localScale = _newRecordBaseScale;

        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            var t = elapsed / duration;
            var scale = Mathf.Lerp(1f, newRecordPopScale, Mathf.Sin(t * Mathf.PI));
            rect.localScale = _newRecordBaseScale * scale;
            yield return null;
        }

        rect.localScale = _newRecordBaseScale;
        _newRecordRoutine = null;
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Result Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var resultCanvas = canvasObject.GetComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        resultCanvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return resultCanvas;
    }

    private static Image CreateOverlay(Transform parent)
    {
        var image = new GameObject("Dark Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = new Color(0.02f, 0.02f, 0.03f, 0.78f);
        Stretch(image.rectTransform);
        return image;
    }

    private static RectTransform CreateContentRoot(Transform parent)
    {
        var root = new GameObject("Result Content", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(620f, 640f);
        return root;
    }

    private static TMP_Text CreateStatText(string name, Transform parent, string label, string value, float x, float y, float valueSize)
    {
        var text = CreateText(name, parent, $"{label}  {value}", valueSize, FontStyles.Normal, new Color(0.92f, 0.94f, 1f), new Vector2(x, y), new Vector2(440f, 44f));
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style, Color color, Vector2 position, Vector2 sizeDelta)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        return text;
    }

    private static UIButton CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIButton));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260f, 48f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);

        var button = buttonObject.GetComponent<UIButton>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.12f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.3f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var labelText = CreateText("Label", buttonObject.transform, label, 24f, FontStyles.Bold, Color.white, Vector2.zero, rect.sizeDelta);
        labelText.raycastTarget = false;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        var minutes = Mathf.FloorToInt(seconds / 60f);
        var wholeSeconds = Mathf.FloorToInt(seconds % 60f);
        var centiseconds = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
        return $"{minutes:00}:{wholeSeconds:00}.{centiseconds:00}";
    }
}
