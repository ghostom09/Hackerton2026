using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Plays the prologue with the same phone-chat flow as the bad ending.</summary>
public sealed class PrologueDialogueController : MonoBehaviour
{
    [Serializable]
    public struct DialogueLine
    {
        [TextArea(2, 4)] public string message;
    }

    [Header("Messages")]
    [SerializeField] private string senderName = "유나";
    [SerializeField] private DialogueLine[] dialogueLines =
    {
        new DialogueLine { message = "날 구하러 와줘." },
        new DialogueLine { message = "오는 길에 갖가지 문제를 풀어서 와줘." },
        new DialogueLine { message = "설마 날 위해 그 정도도 못 해주는 건 아니지?" },
    };

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = .6f;
    [SerializeField] private float chatOpenDelay = .3f;
    [SerializeField] private float firstMessageDelay = .7f;
    [SerializeField] private float lastMessageHoldDuration = .85f;
    [SerializeField, Min(1f)] private float messageScrollSensitivity = 120f;

    [Header("Scene UI References")]
    [Tooltip("Assign the copied BadEnding chat UI here.")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_Text chatTitleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private Image fadePanel;

    private TMP_FontAsset dialogueFont;
    private RectTransform messageContentRect;
    private int messageIndex;
    private int advanceInputAllowedFrame;
    private bool chatOpened;
    private bool changingScene;
    private bool endingScheduled;
    private bool firstMessagePending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForPrologue()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Prologue"
            && FindFirstObjectByType<PrologueDialogueController>() == null)
            new GameObject(nameof(PrologueDialogueController)).AddComponent<PrologueDialogueController>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        HideLegacyDialogueUi();
        EnsureDefaultMessages();
        PrepareCanvas();
        if (HasSceneUiReferences())
            PrepareReferencedUi();
        else
            BuildTemporaryUi();
    }

    private void Start() => StartCoroutine(StartPrologue());

    private void Update()
    {
        if (changingScene)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            changingScene = true;
            LoadInGameScene();
            return;
        }

        if (endingScheduled || firstMessagePending)
            return;

        if (!chatOpened)
            return;

        HandleMessageScroll();
        bool pressSpace = Keyboard.current != null &&
                          (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (Time.frameCount >= advanceInputAllowedFrame && (pressSpace || mouseClick))
            ShowNextMessage();
    }

    private void PrepareCanvas()
    {
        canvas ??= FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("ProloguePhoneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font != null)
            {
                dialogueFont = text.font;
                break;
            }
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
    }

    private bool HasSceneUiReferences()
    {
        return chatPanel != null && messageText != null &&
               messageScrollRect != null && messageScrollRect.content != null && fadePanel != null;
    }

    private void PrepareReferencedUi()
    {
        messageContentRect = messageScrollRect.content;
        ConfigureMessageViewport();
        if (messageText.transform.parent != messageContentRect)
            messageText.transform.SetParent(messageContentRect, false);
        messageScrollRect.horizontal = false;
        messageScrollRect.vertical = true;
        messageScrollRect.scrollSensitivity = messageScrollSensitivity;
        AlignMessageToTopLeft();
        HideScrollBars();

        if (chatTitleText != null)
            chatTitleText.text = SenderName;
        chatPanel.SetActive(false);
        SetImageVisible(fadePanel, true, 1f);
    }

    private void BuildTemporaryUi()
    {
        chatPanel = CreateChatPanel();
        chatPanel.SetActive(false);
        fadePanel = CreateImage("BlackFade", canvas.transform, Color.black);
        Stretch(fadePanel.rectTransform, Vector2.zero, Vector2.one);
        SetImageVisible(fadePanel, true, 1f);
    }

    private GameObject CreateChatPanel()
    {
        Image panel = CreateImage("ChatPanel", canvas.transform, new Color(.93f, .96f, .97f, 1f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(620f, 900f);

        Image header = CreateImage("ChatHeader", panel.transform, new Color(.14f, .27f, .34f, 1f));
        Stretch(header.rectTransform, new Vector2(0f, .88f), Vector2.one);
        chatTitleText = CreateText("ChatTitle", header.transform, SenderName, 38f, TextAlignmentOptions.Center, Color.white, Vector2.zero, new Vector2(480f, 70f));

        GameObject viewportObject = new("MessageViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(panel.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport, Vector2.zero, new Vector2(1f, .88f));
        viewport.offsetMin = new Vector2(34f, 34f);
        viewport.offsetMax = new Vector2(-34f, -24f);
        viewportObject.GetComponent<Image>().color = Color.clear;

        messageContentRect = new GameObject("MessageContent", typeof(RectTransform)).GetComponent<RectTransform>();
        messageContentRect.SetParent(viewport, false);
        messageContentRect.anchorMin = messageContentRect.anchorMax = new Vector2(0f, 1f);
        messageContentRect.pivot = new Vector2(0f, 1f);
        messageContentRect.sizeDelta = new Vector2(viewport.rect.width, 1f);

        messageText = CreateText("MessageText", messageContentRect, string.Empty, 29f, TextAlignmentOptions.TopLeft, new Color(.06f, .09f, .11f), Vector2.zero, new Vector2(1f, 1f));
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageScrollRect = viewportObject.GetComponent<ScrollRect>();
        messageScrollRect.viewport = viewport;
        messageScrollRect.content = messageContentRect;
        messageScrollRect.horizontal = false;
        messageScrollRect.movementType = ScrollRect.MovementType.Clamped;
        AlignMessageToTopLeft();
        return panel.gameObject;
    }

    private IEnumerator StartPrologue()
    {
        // Let the revealed background breathe briefly before the first message arrives.
        yield return FadeImage(fadePanel, 1f, 0f, openingFadeDuration);
        fadePanel.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(chatOpenDelay);
        OpenPhone();
        firstMessagePending = true;
        yield return new WaitForSecondsRealtime(firstMessageDelay);
        ShowNextMessage();
        firstMessagePending = false;
    }

    public void OpenPhone()
    {
        if (chatOpened)
            return;

        chatOpened = true;
        chatPanel.SetActive(true);
        messageText.text = string.Empty;
        messageIndex = 0;
        advanceInputAllowedFrame = Time.frameCount + 1;
    }

    private void ShowNextMessage()
    {
        if (messageIndex >= dialogueLines.Length)
            return;

        DialogueLine line = dialogueLines[messageIndex++];
        if (!string.IsNullOrEmpty(messageText.text))
            messageText.text += "\n\n";
        messageText.text += $"<b>{SenderName}</b>\n{line.message}";
        ResizeMessageContent();

        if (messageIndex >= dialogueLines.Length)
            StartCoroutine(WaitForLastMessageThenLoad());
    }

    private IEnumerator WaitForLastMessageThenLoad()
    {
        endingScheduled = true;
        yield return new WaitForSecondsRealtime(lastMessageHoldDuration);
        yield return FadeOutAndLoadScene();
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        if (changingScene)
            yield break;

        changingScene = true;
        yield return FadeImage(fadePanel, 0f, 1f, openingFadeDuration);
        LoadInGameScene();
    }

    private static void LoadInGameScene()
    {
        if (SceneManager.Instance != null)
            SceneManager.Instance.ChangeScene(SceneName.InGameScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene");
    }

    private void HandleMessageScroll()
    {
        if (messageScrollRect == null || Mouse.current == null)
            return;
        float wheelY = Mouse.current.scroll.ReadValue().y;
        if (!Mathf.Approximately(wheelY, 0f))
            messageScrollRect.verticalNormalizedPosition = Mathf.Clamp01(messageScrollRect.verticalNormalizedPosition + wheelY * .0025f);
    }

    private void ResizeMessageContent()
    {
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(1f, messageScrollRect.viewport.rect.width);
        float height = Mathf.Max(1f, messageText.preferredHeight + 24f);
        messageText.rectTransform.sizeDelta = new Vector2(width, height);
        messageContentRect.sizeDelta = new Vector2(width, height);
        Canvas.ForceUpdateCanvases();
        messageScrollRect.verticalNormalizedPosition = 0f;
    }

    private void AlignMessageToTopLeft()
    {
        messageContentRect.anchorMin = messageContentRect.anchorMax = new Vector2(0f, 1f);
        messageContentRect.pivot = new Vector2(0f, 1f);
        messageContentRect.anchoredPosition = Vector2.zero;
        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        messageText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void ConfigureMessageViewport()
    {
        if (messageScrollRect.viewport == null)
            return;
        RectTransform viewport = messageScrollRect.viewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(16f, 65f);
        viewport.offsetMax = new Vector2(-16f, -100f);
    }

    private void HideScrollBars()
    {
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

    private void HideLegacyDialogueUi()
    {
        foreach (TextMeshProUGUI text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.name is "Name" or "Talk" or "SpeakerName" or "Dialogue")
                (text.transform.parent != null ? text.transform.parent.gameObject : text.gameObject).SetActive(false);
        }
    }

    private void EnsureDefaultMessages()
    {
        if (dialogueLines is { Length: > 0 })
            return;
        dialogueLines = new[] { new DialogueLine { message = "날 구하러 와줘." } };
    }

    private string SenderName => string.IsNullOrWhiteSpace(senderName) ? "유나" : senderName;

    private IEnumerator FadeImage(Image target, float startAlpha, float endAlpha, float duration)
    {
        target.transform.SetAsLastSibling();
        target.gameObject.SetActive(true);
        SetImageAlpha(target, startAlpha);
        for (float elapsed = 0f; elapsed < Mathf.Max(.01f, duration); elapsed += Time.unscaledDeltaTime)
        {
            SetImageAlpha(target, Mathf.Lerp(startAlpha, endAlpha, elapsed / Mathf.Max(.01f, duration)));
            yield return null;
        }
        SetImageAlpha(target, endAlpha);
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = dialogueFont != null ? dialogueFont : TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = dimensions;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetImageVisible(Image image, bool active, float alpha)
    {
        image.gameObject.SetActive(active);
        SetImageAlpha(image, alpha);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
