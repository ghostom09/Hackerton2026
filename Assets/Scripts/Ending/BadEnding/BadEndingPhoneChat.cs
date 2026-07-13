using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Phone-sized presentation used by the bad-ending sequence.</summary>
public sealed class BadEndingPhoneChat : MonoBehaviour
{
    private TMP_Text _messageText;
    private TMP_Text _statusText;
    private GameObject _canvasRoot;
    private string _messages = string.Empty;

    private void Awake() => BuildUi();

    public void ShowPhone()
    {
        _canvasRoot.SetActive(true);
        _messages = string.Empty;
        _messageText.text = string.Empty;
        _statusText.text = "연결됨";
    }

    public void AddIncomingMessage(string message)
    {
        _messages += string.IsNullOrEmpty(_messages) ? message : "\n\n" + message;
        _messageText.text = _messages;
    }

    public void ShowDisconnected()
    {
        _statusText.text = "연결이 끊어졌습니다";
        _messageText.text = _messages + "\n\n<color=#8A1F1F>연결이 끊어졌습니다.</color>";
    }

    public void HidePhone() => _canvasRoot.SetActive(false);

    private void BuildUi()
    {
        _canvasRoot = new GameObject("Phone", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasRoot.transform.SetParent(transform, false);
        var canvas = _canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = _canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        var screen = Image("PhoneScreen", _canvasRoot.transform, new Color(0.95f, 0.94f, 0.91f));
        Stretch(screen.rectTransform, new Vector2(.18f, .06f), new Vector2(.82f, .94f));
        var header = Image("Header", screen.transform, new Color(.12f, .15f, .2f));
        Stretch(header.rectTransform, new Vector2(0, .88f), Vector2.one);
        var name = Text("FavoriteName", header.transform, "최애", 42, Color.white, TextAlignmentOptions.Center);
        Stretch(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
        _statusText = Text("ConnectionStatus", screen.transform, "연결됨", 24, new Color(.35f, .35f, .35f), TextAlignmentOptions.Center);
        Stretch(_statusText.rectTransform, new Vector2(.08f, .81f), new Vector2(.92f, .87f));
        _messageText = Text("LastMessages", screen.transform, string.Empty, 34, new Color(.08f, .08f, .1f), TextAlignmentOptions.TopLeft);
        _messageText.enableWordWrapping = true;
        Stretch(_messageText.rectTransform, new Vector2(.09f, .13f), new Vector2(.91f, .78f));
        var hint = Text("MessageHint", screen.transform, "읽지 않은 메시지", 24, new Color(.45f, .45f, .45f), TextAlignmentOptions.Center);
        Stretch(hint.rectTransform, new Vector2(.05f, .05f), new Vector2(.95f, .11f));
    }

    private static Image Image(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
    }
}
