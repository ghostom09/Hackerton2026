using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

public class BadEndingController : MonoBehaviour
{
    [Header("Replaceable Art")]
    [SerializeField] private Sprite roomBackgroundSprite;
    [SerializeField] private Sprite phoneSprite;
    [SerializeField] private Sprite glitchSprite;

    [Header("Sound")]
    [SerializeField] private AudioClip phoneRingSound;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 2f;
    [SerializeField] private float ringDelay = 2f;
    [SerializeField] private float glitchDuration = 2f;

    [Header("Messages")]
    [SerializeField] private string senderName = "유나";

    [TextArea(2, 5)]
    [SerializeField] private string[] messages;

    private Canvas canvas;
    private TMP_FontAsset dialogueFont;
    private UIButton phoneButton;
    private RectTransform phoneRect;
    private GameObject chatPanel;
    private TMP_Text messageText;
    private CanvasGroup fadePanel;
    private CanvasGroup glitchPanel;
    private AudioSource audioSource;

    private int messageIndex;
    private bool chatOpened;
    private bool glitchPlaying;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureDefaultMessages();
        PrepareCanvas();
        BuildTemporaryUI();
    }

    private void Start()
    {
        StartCoroutine(StartBadEnding());
    }

    private void Update()
    {
        if (!chatOpened || glitchPlaying)
            return;

        bool pressSpace = Keyboard.current != null &&
                          Keyboard.current.spaceKey.wasPressedThisFrame;

        bool mouseClick = Mouse.current != null &&
                          Mouse.current.leftButton.wasPressedThisFrame;

        if (pressSpace || mouseClick)
            ShowNextMessage();
    }

    private void PrepareCanvas()
    {
        canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new(
                "BadEndingCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        TMP_Text[] oldTexts = canvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text oldText in oldTexts)
        {
            if (oldText != null && oldText.font != null)
            {
                dialogueFont = oldText.font;
                break;
            }
        }

        for (int i = 0; i < canvas.transform.childCount; i++)
            canvas.transform.GetChild(i).gameObject.SetActive(false);

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void BuildTemporaryUI()
    {
        Image roomBackground = CreateImage(
            "RoomBackground",
            canvas.transform,
            roomBackgroundSprite,
            new Color(0.16f, 0.17f, 0.2f, 1f));
        Stretch(roomBackground.rectTransform);

        GameObject phoneObject = new(
            "PhoneButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(UIButton));
        phoneObject.transform.SetParent(canvas.transform, false);

        phoneRect = phoneObject.GetComponent<RectTransform>();
        phoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        phoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        phoneRect.pivot = new Vector2(0.5f, 0.5f);
        phoneRect.anchoredPosition = new Vector2(0f, -190f);
        phoneRect.sizeDelta = new Vector2(220f, 130f);

        Image phoneImage = phoneObject.GetComponent<Image>();
        phoneImage.sprite = phoneSprite;
        phoneImage.color = phoneSprite != null
            ? Color.white
            : new Color(0.08f, 0.09f, 0.11f, 1f);
        phoneImage.preserveAspect = phoneSprite != null;

        phoneButton = phoneObject.GetComponent<UIButton>();
        phoneButton.interactable = false;
        phoneButton.onClick.AddListener(OpenPhone);

        CreateText(
            "PhoneLabel",
            phoneObject.transform,
            "PHONE\nCLICK",
            28f,
            TextAlignmentOptions.Center,
            Color.white,
            Vector2.zero,
            phoneRect.sizeDelta);

        chatPanel = CreateChatPanel();
        chatPanel.SetActive(false);

        Image glitchImage = CreateImage(
            "GlitchPanel",
            canvas.transform,
            glitchSprite,
            Color.black);
        Stretch(glitchImage.rectTransform);
        glitchPanel = glitchImage.gameObject.AddComponent<CanvasGroup>();
        SetCanvasGroup(glitchPanel, false, 0f);

        Image fadeImage = CreateImage(
            "BlackFadePanel",
            canvas.transform,
            null,
            Color.black);
        Stretch(fadeImage.rectTransform);
        fadePanel = fadeImage.gameObject.AddComponent<CanvasGroup>();
        SetCanvasGroup(fadePanel, true, 1f);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    private GameObject CreateChatPanel()
    {
        Image panelImage = CreateImage(
            "ChatPanel",
            canvas.transform,
            null,
            new Color(0.76f, 0.83f, 0.88f, 1f));

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 920f);

        CreateText(
            "ChatTitle",
            panelRect,
            senderName,
            38f,
            TextAlignmentOptions.Center,
            new Color(0.08f, 0.09f, 0.11f),
            new Vector2(0f, 410f),
            new Vector2(680f, 70f));

        messageText = CreateText(
            "MessageText",
            panelRect,
            string.Empty,
            31f,
            TextAlignmentOptions.TopLeft,
            new Color(0.08f, 0.09f, 0.11f),
            new Vector2(0f, -20f),
            new Vector2(650f, 760f));
        messageText.textWrappingMode = TextWrappingModes.Normal;

        return panelImage.gameObject;
    }

    private IEnumerator StartBadEnding()
    {
        yield return Fade(fadePanel, 1f, 0f, openingFadeDuration);
        fadePanel.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(ringDelay);

        if (phoneRingSound != null)
        {
            audioSource.clip = phoneRingSound;
            audioSource.Play();
        }

        phoneButton.interactable = true;
        StartCoroutine(ShakePhoneWhileRinging());
    }

    private IEnumerator ShakePhoneWhileRinging()
    {
        Vector2 basePosition = phoneRect.anchoredPosition;

        while (!chatOpened)
        {
            phoneRect.anchoredPosition = basePosition + new Vector2(
                Random.Range(-7f, 7f),
                Random.Range(-3f, 3f));
            yield return new WaitForSecondsRealtime(0.05f);
        }

        phoneRect.anchoredPosition = basePosition;
    }

    public void OpenPhone()
    {
        if (chatOpened)
            return;

        chatOpened = true;
        phoneButton.interactable = false;
        phoneButton.gameObject.SetActive(false);
        audioSource.Stop();

        chatPanel.SetActive(true);
        messageText.text = string.Empty;
        messageIndex = 0;
        ShowNextMessage();
    }

    private void ShowNextMessage()
    {
        if (messageIndex >= messages.Length)
        {
            StartCoroutine(PlayGlitch());
            return;
        }

        if (!string.IsNullOrEmpty(messageText.text))
            messageText.text += "\n\n";

        messageText.text +=
            $"<b>{senderName}</b>\n{messages[messageIndex]}";
        messageIndex++;
    }

    private IEnumerator PlayGlitch()
    {
        if (glitchPlaying)
            yield break;

        glitchPlaying = true;
        chatPanel.SetActive(false);
        SetCanvasGroup(glitchPanel, true, 1f);

        RectTransform glitchRect = glitchPanel.GetComponent<RectTransform>();
        float elapsedTime = 0f;

        while (elapsedTime < glitchDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            glitchPanel.alpha = Random.Range(0.25f, 1f);

            if (glitchSprite == null)
            {
                Image image = glitchPanel.GetComponent<Image>();
                float value = Random.value > 0.35f ? 0f : 1f;
                image.color = new Color(value, value, value, 1f);
            }

            glitchRect.anchoredPosition = new Vector2(
                Random.Range(-30f, 30f),
                Random.Range(-18f, 18f));

            yield return new WaitForSecondsRealtime(0.04f);
        }

        LoadMainMenu();
    }

    private static void LoadMainMenu()
    {
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.ChangeScene(SceneName.MainMenu);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private IEnumerator Fade(
        CanvasGroup target,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        target.gameObject.SetActive(true);
        target.alpha = startAlpha;
        target.blocksRaycasts = true;

        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            target.alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                elapsedTime / safeDuration);
            yield return null;
        }

        target.alpha = endAlpha;
        target.blocksRaycasts = endAlpha > 0f;
    }

    private Image CreateImage(
        string objectName,
        Transform parent,
        Sprite sprite,
        Color color)
    {
        Image image = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)).GetComponent<Image>();

        image.transform.SetParent(parent, false);
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        return image;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 position,
        Vector2 size)
    {
        TextMeshProUGUI label = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();

        label.transform.SetParent(parent, false);
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;

        if (dialogueFont != null)
            label.font = dialogueFont;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return label;
    }

    private void EnsureDefaultMessages()
    {
        if (messages != null && messages.Length > 0)
            return;

        messages = new[]
        {
            "...... 올 때가 한참 지난 것 같은데",
            "결국 안 오는 건가.....",
            "더 이상 못 만나는 거겠지...?",
            "그럼 안녕..."
        };
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetCanvasGroup(
        CanvasGroup group,
        bool active,
        float alpha)
    {
        group.gameObject.SetActive(active);
        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;
    }
}
