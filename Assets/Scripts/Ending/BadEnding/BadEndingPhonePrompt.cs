using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

/// <summary>Clickable ringing phone shown after the in-game timer expires.</summary>
public sealed class BadEndingPhonePrompt : MonoBehaviour
{
    private GameObject canvasRoot;
    private RectTransform phone;
    private AudioSource audioSource;
    private Action onAnswered;
    private Coroutine ringRoutine;

    public void Show(Action answered)
    {
        BuildIfNeeded();
        onAnswered = answered;
        canvasRoot.SetActive(true);
        if (ringRoutine != null) StopCoroutine(ringRoutine);
        ringRoutine = StartCoroutine(RingRoutine());
    }

    private void OnDestroy()
    {
        if (ringRoutine != null) StopCoroutine(ringRoutine);
    }

    private void BuildIfNeeded()
    {
        if (canvasRoot != null) return;

        canvasRoot = new GameObject("BadEndingPhone", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var buttonObject = new GameObject("AnswerPhone", typeof(RectTransform), typeof(Image), typeof(UIButton));
        buttonObject.transform.SetParent(canvasRoot.transform, false);
        phone = buttonObject.GetComponent<RectTransform>();
        phone.anchorMin = phone.anchorMax = new Vector2(.5f, .5f);
        phone.anchoredPosition = Vector2.zero;
        phone.sizeDelta = new Vector2(250f, 420f);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(.08f, .12f, .18f, .98f);
        var button = buttonObject.GetComponent<UIButton>();
        button.targetGraphic = image;
        button.onClick.AddListener(Answer);

        var title = new GameObject("Caller", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        title.transform.SetParent(buttonObject.transform, false);
        title.text = "전화가 울립니다";
        title.font = TMP_Settings.defaultFontAsset;
        title.fontSize = 30f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        Stretch(title.rectTransform, new Vector2(.08f, .62f), new Vector2(.92f, .85f));

        var hint = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        hint.transform.SetParent(buttonObject.transform, false);
        hint.text = "클릭해서 받기";
        hint.font = TMP_Settings.defaultFontAsset;
        hint.fontSize = 24f;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(.7f, .9f, 1f);
        Stretch(hint.rectTransform, new Vector2(.08f, .16f), new Vector2(.92f, .34f));

        audioSource = buttonObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        canvasRoot.SetActive(false);
    }

    private IEnumerator RingRoutine()
    {
        var baseScale = phone.localScale;
        while (true)
        {
            PlayRingTone();
            for (float elapsed = 0f; elapsed < .16f; elapsed += Time.unscaledDeltaTime)
            {
                phone.localScale = Vector3.Lerp(baseScale, baseScale * 1.08f, elapsed / .16f);
                yield return null;
            }
            for (float elapsed = 0f; elapsed < .16f; elapsed += Time.unscaledDeltaTime)
            {
                phone.localScale = Vector3.Lerp(baseScale * 1.08f, baseScale, elapsed / .16f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.48f);
        }
    }

    private void Answer()
    {
        if (ringRoutine != null) StopCoroutine(ringRoutine);
        ringRoutine = null;
        if (audioSource != null) audioSource.Stop();
        canvasRoot.SetActive(false);
        onAnswered?.Invoke();
    }

    private void PlayRingTone()
    {
        const int sampleRate = 44100;
        const float duration = .12f;
        var samples = new float[Mathf.CeilToInt(sampleRate * duration)];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / sampleRate) * .15f;
        var clip = AudioClip.Create("PhoneRing", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
