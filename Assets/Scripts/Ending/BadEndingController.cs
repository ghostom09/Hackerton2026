using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

public class BadEndingController : MonoBehaviour
{
    private const int ChatMessageCountBeforeImage = 20;
    private const int LastImageSequenceMessage = 22;

    private enum DialogueStage
    {
        Chat,
        ImageSequence
    }

    [Header("Replaceable Art")]
    [SerializeField] private Sprite roomBackgroundSprite;
    [SerializeField] private Sprite phoneSprite;
    [SerializeField] private Sprite glitchSprite;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 2f;
    [SerializeField] private float ringDelay = 2f;
    [SerializeField] private float firstMessageDelay = .7f;
    [SerializeField] private float imageSequenceGlitchDuration = .3f;
    [SerializeField] private float imageSequenceLineDuration = 1.5f;
    [SerializeField] private float endingFadeDuration = .5f;
    [SerializeField, Min(1f)] private float messageScrollSensitivity = 120f;

    [Header("Messages")]
    [SerializeField] private string senderName = "유나";

    [Tooltip("One chat message per entry. Entries are shown one at a time on click or Space.")]
    [TextArea(2, 5)]
    [SerializeField] private string[] messages;

    [Header("Scene UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image roomBackgroundImage;
    [SerializeField] private Image phoneImage;
    [SerializeField] private UIButton phoneButton;
    [SerializeField] private RectTransform phoneRect;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_Text chatTitleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private Image fadePanel;
    [SerializeField] private Image glitchPanel;

    [Header("Image Sequence UI")]
    [SerializeField] private GameObject imageSequencePanel;
    [SerializeField] private Image storyImage;
    [SerializeField] private TMP_Text imageSequenceText;

    private TMP_FontAsset dialogueFont;
    private RectTransform messageContentRect;

    private int messageIndex;
    private int advanceInputAllowedFrame;
    private bool chatOpened;
    private bool glitchPlaying;
    private bool imageTransitionReady;
    private bool firstMessagePending;
    private DialogueStage dialogueStage;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureDefaultMessages();
        PrepareCanvas();
        if (HasSceneUiReferences())
            PrepareReferencedUI();
        else
            BuildTemporaryUI();
    }

    private void Start()
    {
        StartCoroutine(StartBadEnding());
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StopAllCoroutines();
            glitchPlaying = true;
            LoadMainMenu();
            return;
        }

        if (!chatOpened || glitchPlaying || firstMessagePending)
            return;

        HandleMessageScroll();

        bool pressSpace = Keyboard.current != null &&
                          Keyboard.current.spaceKey.wasPressedThisFrame;

        bool mouseClick = Mouse.current != null &&
                          Mouse.current.leftButton.wasPressedThisFrame;

        if (Time.frameCount < advanceInputAllowedFrame)
            return;

        // Clicking anywhere on the phone/chat, as well as Space, advances the dialogue.
        if (pressSpace || mouseClick)
            ShowNextMessage();
    }

    private void HandleMessageScroll()
    {
        if (messageScrollRect == null || Mouse.current == null)
            return;

        float wheelY = Mouse.current.scroll.ReadValue().y;
        if (!Mathf.Approximately(wheelY, 0f))
            messageScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                messageScrollRect.verticalNormalizedPosition + wheelY * .0025f);
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

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private bool HasSceneUiReferences()
    {
        return phoneButton != null && chatPanel != null && messageText != null &&
               messageScrollRect != null && messageScrollRect.content != null &&
               fadePanel != null && glitchPanel != null;
    }

    private void PrepareReferencedUI()
    {
        phoneRect ??= phoneButton.GetComponent<RectTransform>();
        messageContentRect = messageScrollRect.content;
        ConfigureMessageViewport();
        if (messageText.transform.parent != messageContentRect)
            messageText.transform.SetParent(messageContentRect, false);
        messageScrollRect.horizontal = false;
        messageScrollRect.vertical = true;
        messageScrollRect.scrollSensitivity = messageScrollSensitivity;
        AlignMessageToTopLeft();
        HideScrollBar();

        if (roomBackgroundImage != null && roomBackgroundSprite != null)
            roomBackgroundImage.sprite = roomBackgroundSprite;
        if (phoneImage != null && phoneSprite != null)
            phoneImage.sprite = phoneSprite;
        if (glitchSprite != null)
            glitchPanel.sprite = glitchSprite;
        if (chatTitleText != null)
            chatTitleText.text = senderName;

        phoneButton.onClick.AddListener(OpenPhone);
        phoneButton.interactable = false;
        chatPanel.SetActive(false);
        SetImageVisible(glitchPanel, false, 0f);
        SetImageVisible(fadePanel, true, 1f);
        EnsureImageSequenceUi();
    }

    private void BuildTemporaryUI()
    {
        roomBackgroundImage = CreateImage(
            "RoomBackground",
            canvas.transform,
            roomBackgroundSprite,
            new Color(0.16f, 0.17f, 0.2f, 1f));
        Stretch(roomBackgroundImage.rectTransform);

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

        phoneImage = phoneObject.GetComponent<Image>();
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

        EnsureImageSequenceUi();

        Image glitchImage = CreateImage(
            "GlitchPanel",
            canvas.transform,
            glitchSprite,
            Color.black);
        Stretch(glitchImage.rectTransform);
        glitchPanel = glitchImage;
        SetImageVisible(glitchPanel, false, 0f);

        Image fadeImage = CreateImage(
            "BlackFadePanel",
            canvas.transform,
            null,
            Color.black);
        Stretch(fadeImage.rectTransform);
        fadePanel = fadeImage;
        SetImageVisible(fadePanel, true, 1f);
    }

    private void EnsureImageSequenceUi()
    {
        if (imageSequencePanel == null || imageSequenceText == null)
            imageSequencePanel = CreateImageSequencePanel();

        imageSequencePanel.SetActive(false);
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

        chatTitleText = CreateText(
            "ChatTitle",
            panelRect,
            senderName,
            38f,
            TextAlignmentOptions.Center,
            new Color(0.08f, 0.09f, 0.11f),
            new Vector2(0f, 410f),
            new Vector2(680f, 70f));

        GameObject viewportObject = new(
            "MessageViewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        viewportObject.transform.SetParent(panelRect, false);

        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = viewportRect.anchorMax = new Vector2(.5f, .5f);
        viewportRect.pivot = new Vector2(.5f, .5f);
        viewportRect.anchoredPosition = new Vector2(0f, -45f);
        viewportRect.sizeDelta = new Vector2(650f, 720f);
        viewportObject.GetComponent<Image>().color = Color.clear;

        GameObject contentObject = new("MessageContent", typeof(RectTransform));
        contentObject.transform.SetParent(viewportRect, false);
        messageContentRect = contentObject.GetComponent<RectTransform>();
        messageContentRect.anchorMin = messageContentRect.anchorMax = new Vector2(0f, 1f);
        messageContentRect.pivot = new Vector2(0f, 1f);
        messageContentRect.anchoredPosition = new Vector2(16f, 0f);
        messageContentRect.sizeDelta = new Vector2(618f, 1f);

        messageText = CreateText(
            "MessageText",
            messageContentRect,
            string.Empty,
            31f,
            TextAlignmentOptions.TopLeft,
            new Color(0.08f, 0.09f, 0.11f),
            Vector2.zero,
            Vector2.zero);
        RectTransform messageRect = messageText.rectTransform;
        messageRect.anchorMin = messageRect.anchorMax = new Vector2(0f, 1f);
        messageRect.pivot = new Vector2(0f, 1f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(618f, 1f);
        messageText.textWrappingMode = TextWrappingModes.Normal;

        messageScrollRect = viewportObject.GetComponent<ScrollRect>();
        messageScrollRect.viewport = viewportRect;
        messageScrollRect.content = messageContentRect;
        messageScrollRect.horizontal = false;
        messageScrollRect.scrollSensitivity = messageScrollSensitivity;
        messageScrollRect.movementType = ScrollRect.MovementType.Clamped;
        AlignMessageToTopLeft();
        HideScrollBar();

        return panelImage.gameObject;
    }

    private GameObject CreateImageSequencePanel()
    {
        Image panel = CreateImage("ImageSequencePanel", canvas.transform, null, new Color(.04f, .05f, .07f, 1f));
        Stretch(panel.rectTransform);

        storyImage = CreateImage("StoryImage", panel.transform, null, new Color(.25f, .28f, .32f, 1f));
        Stretch(storyImage.rectTransform, new Vector2(.08f, .2f), new Vector2(.52f, .8f));

        imageSequenceText = CreateText("StoryText", panel.transform, string.Empty, 36f, TextAlignmentOptions.MidlineLeft, Color.white, Vector2.zero, Vector2.zero);
        Stretch(imageSequenceText.rectTransform, new Vector2(.57f, .2f), new Vector2(.92f, .8f));
        return panel.gameObject;
    }

    private IEnumerator StartBadEnding()
    {
        yield return FadeImage(fadePanel, 1f, 0f, openingFadeDuration);
        fadePanel.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(ringDelay);

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

        chatPanel.SetActive(true);
        messageText.text = string.Empty;
        messageIndex = 0;
        StartCoroutine(ShowFirstMessageAfterDelay());
    }

    private IEnumerator ShowFirstMessageAfterDelay()
    {
        firstMessagePending = true;
        yield return new WaitForSecondsRealtime(firstMessageDelay);
        ShowNextMessage();
        firstMessagePending = false;
        advanceInputAllowedFrame = Time.frameCount + 1;
    }

    private void ShowNextMessage()
    {
        // The 20th line remains visible in the chat. The following input starts
        // the fade transition, so only lines 21 and 22 belong to the image sequence.
        if (dialogueStage == DialogueStage.Chat && imageTransitionReady)
        {
            StartCoroutine(SwitchToImageSequence());
            return;
        }

        if (messageIndex >= messages.Length)
            return;

        if (dialogueStage == DialogueStage.Chat)
        {
            if (!string.IsNullOrEmpty(messageText.text))
                messageText.text += "\n\n";

            messageText.text += $"<b>{senderName}</b>\n{messages[messageIndex++]}";
            ResizeMessageContent();

            if (messageIndex == ChatMessageCountBeforeImage)
                imageTransitionReady = true;
        }
    }

    private IEnumerator SwitchToImageSequence()
    {
        if (glitchPlaying)
            yield break;

        glitchPlaying = true;
        imageTransitionReady = false;
        yield return PlayGlitchVisual(imageSequenceGlitchDuration);
        chatPanel.SetActive(false);
        imageSequencePanel.SetActive(true);
        imageSequenceText.text = string.Empty;
        dialogueStage = DialogueStage.ImageSequence;
        yield return PlayImageSequenceCutscene();
    }

    private IEnumerator PlayGlitchVisual(float duration)
    {
        if (glitchPanel == null)
            yield break;

        SetImageVisible(glitchPanel, true, 1f);
        RectTransform glitchRect = glitchPanel.rectTransform;
        Vector2 basePosition = glitchRect.anchoredPosition;
        float safeDuration = Mathf.Max(.01f, duration);

        for (float elapsed = 0f; elapsed < safeDuration; elapsed += Time.unscaledDeltaTime)
        {
            Color color = glitchPanel.color;
            color.a = UnityEngine.Random.Range(.25f, 1f);
            glitchPanel.color = color;
            if (glitchSprite == null)
            {
                float value = UnityEngine.Random.value > .35f ? 0f : 1f;
                glitchPanel.color = new Color(value, value, value, color.a);
            }
            glitchRect.anchoredPosition = basePosition + new Vector2(
                UnityEngine.Random.Range(-30f, 30f),
                UnityEngine.Random.Range(-18f, 18f));
            yield return new WaitForSecondsRealtime(.04f);
        }

        glitchRect.anchoredPosition = basePosition;
        SetImageVisible(glitchPanel, false, 0f);
    }

    private IEnumerator PlayImageSequenceCutscene()
    {
        int lastImageMessageIndex = Mathf.Min(LastImageSequenceMessage, messages.Length);
        while (messageIndex < lastImageMessageIndex)
        {
            imageSequenceText.text = messages[messageIndex++];
            yield return new WaitForSecondsRealtime(imageSequenceLineDuration);
        }

        yield return FadeImage(fadePanel, 0f, 1f, endingFadeDuration);
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

    private IEnumerator FadeImage(
        Image target,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        target.gameObject.SetActive(true);
        SetImageAlpha(target, startAlpha);
        target.raycastTarget = true;

        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            SetImageAlpha(target, Mathf.Lerp(startAlpha, endAlpha, elapsedTime / safeDuration));
            yield return null;
        }

        SetImageAlpha(target, endAlpha);
        target.raycastTarget = endAlpha > 0f;
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
        else
            label.font = TMP_Settings.defaultFontAsset;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return label;
    }

    private void ResizeMessageContent()
    {
        if (messageText == null || messageContentRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        float height = Mathf.Max(1f, messageText.preferredHeight + 24f);
        float width = messageScrollRect != null && messageScrollRect.viewport != null
            ? Mathf.Max(1f, messageScrollRect.viewport.rect.width - 32f)
            : 618f;
        messageText.rectTransform.sizeDelta = new Vector2(width, height);
        messageContentRect.sizeDelta = new Vector2(width, height);
        Canvas.ForceUpdateCanvases();

        if (messageScrollRect != null)
            messageScrollRect.verticalNormalizedPosition = 0f;
    }

    private void AlignMessageToTopLeft()
    {
        if (messageText == null || messageContentRect == null)
            return;

        messageContentRect.anchorMin = messageContentRect.anchorMax = new Vector2(0f, 1f);
        messageContentRect.pivot = new Vector2(0f, 1f);
        messageContentRect.anchoredPosition = new Vector2(16f, 0f);

        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        messageText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void ConfigureMessageViewport()
    {
        if (messageScrollRect == null || messageScrollRect.viewport == null)
            return;

        RectTransform viewport = messageScrollRect.viewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(16f, 65f);
        // Keep the sender title area free at the top of the chat panel.
        viewport.offsetMax = new Vector2(-16f, -100f);
    }

    private void HideScrollBar()
    {
        if (messageScrollRect == null)
            return;

        if (messageScrollRect.verticalScrollbar != null)
        {
            messageScrollRect.verticalScrollbar.gameObject.SetActive(false);
            messageScrollRect.verticalScrollbar = null;
        }

        if (messageScrollRect.horizontalScrollbar != null)
        {
            messageScrollRect.horizontalScrollbar.gameObject.SetActive(false);
            messageScrollRect.horizontalScrollbar = null;
        }
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

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetImageVisible(
        Image image,
        bool active,
        float alpha)
    {
        image.gameObject.SetActive(active);
        SetImageAlpha(image, alpha);
        image.raycastTarget = active;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
