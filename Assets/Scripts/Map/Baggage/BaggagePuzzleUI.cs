using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BaggagePuzzleUI : MonoBehaviour
{
    [SerializeField] private Text missionText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button backButton;

    private BaggagePuzzleController controller;

    public void BuildDefaultLayout()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        if (missionText != null && progressText != null && stateText != null && backButton != null)
            return;

        Font font = ResolveDefaultFont();

        RectTransform panel = CreatePanel("Mission Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(520f, 132f));
        missionText = CreateText("Mission Text", panel, font, 30, TextAnchor.UpperLeft, new Vector2(20f, -18f), new Vector2(480f, 42f));
        progressText = CreateText("Progress Text", panel, font, 24, TextAnchor.MiddleLeft, new Vector2(20f, -62f), new Vector2(240f, 34f));
        stateText = CreateText("State Text", panel, font, 22, TextAnchor.MiddleLeft, new Vector2(20f, -96f), new Vector2(300f, 28f));

        backButton = CreateButton("Back Button", panel, font, new Vector2(356f, -76f), new Vector2(132f, 42f), "\uB4A4\uB85C");
    }

    public void Bind(BaggagePuzzleController puzzleController)
    {
        controller = puzzleController;

        if (backButton == null)
            return;

        //backButton.onClick.RemoveAllListeners();
        //backButton.onClick.AddListener(ReturnToStart);
    }

    public void SetMission(string text)
    {
        if (missionText != null)
            missionText.text = text;
    }

    public void SetProgress(int safeCount, int totalCount, bool isCleared)
    {
        if (progressText != null)
            progressText.text = $"\uC548\uC804 \uAD6C\uC5ED: {safeCount}/{totalCount}";

        if (stateText != null)
            stateText.text = isCleared ? "\uC644\uB8CC" : "\uBE68\uAC04 \uBAA9\uD45C \uC9C0\uC810\uC73C\uB85C \uBAA8\uB450 \uC62E\uAE30\uC138\uC694";
    }

    public void SetCanUndo(bool canUndo)
    {
        //if (backButton != null)
            //backButton.interactable = canUndo;
    }

    private void ReturnToStart()
    {
        if (controller != null)
            controller.TryReturnToStart();
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    private RectTransform CreatePanel(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(transform, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 0.82f);

        return rect;
    }

    private Text CreateText(string objectName, Transform parent, Font font, int size, TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        return text;
    }

    private Button CreateButton(string objectName, Transform parent, Font font, Vector2 anchoredPosition, Vector2 sizeDelta, string label)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.86f, 0.9f, 0.95f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        Text text = CreateText("Label", buttonObject.transform, font, 22, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.color = new Color(0.05f, 0.07f, 0.09f, 1f);
        text.text = label;
        text.raycastTarget = false;

        return button;
    }

    private Font ResolveDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }
}
