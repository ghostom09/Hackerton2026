using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject[] countdownObjects;
    [SerializeField] private float stepDuration = 0.65f;
    [SerializeField] private bool autoCreateIfEmpty = true;

    private Canvas _runtimeCanvas;

    private void Awake()
    {
        if ((countdownObjects == null || countdownObjects.Length == 0) && autoCreateIfEmpty)
            CreateRuntimeCountdownObjects();

        HideAll();
    }

    public IEnumerator Play()
    {
        if (countdownObjects == null || countdownObjects.Length == 0)
            yield break;

        HideAll();

        foreach (var item in countdownObjects)
        {
            if (item == null)
                continue;

            item.SetActive(true);
            yield return new WaitForSecondsRealtime(stepDuration);
            item.SetActive(false);
        }
    }

    private void HideAll()
    {
        if (countdownObjects == null)
            return;

        foreach (var item in countdownObjects)
        {
            if (item != null)
                item.SetActive(false);
        }
    }

    private void CreateRuntimeCountdownObjects()
    {
        _runtimeCanvas = new GameObject("Countdown Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        _runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _runtimeCanvas.sortingOrder = 25000;

        var scaler = _runtimeCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var labels = new[] { "3", "2", "1", "Go!" };
        countdownObjects = new GameObject[labels.Length];

        for (var i = 0; i < labels.Length; i++)
            countdownObjects[i] = CreateLabel(labels[i]).gameObject;
    }

    private TMP_Text CreateLabel(string label)
    {
        var text = new GameObject($"Countdown {label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(_runtimeCanvas.transform, false);
        text.text = label;
        text.fontSize = label == "Go!" ? 180f : 220f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
