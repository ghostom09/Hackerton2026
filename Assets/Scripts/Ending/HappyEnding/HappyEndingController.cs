using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HappyEndingController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject finalDoor;
    [SerializeField] private Transform favoriteCharacter;
    [SerializeField] private Transform favoriteApproachTarget;
    [SerializeField] private Camera endingCamera;
    [SerializeField] private Camera bedroomCamera;
    [SerializeField] private GameObject bed;
    [SerializeField] private GameObject waterCup;
    [SerializeField] private WaterCupInteraction waterCupInteraction;
    [SerializeField] private GameObject happyPhoto;
    [SerializeField] private GameObject badPhoto;
    [SerializeField] private GameObject phone;
    [SerializeField] private Image brightFadeImage;
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text phoneMessageText;
    [SerializeField] private GameObject resultUI;
    [SerializeField] private UnityEvent onFinalDoorOpened;
    [SerializeField] private UnityEvent onPlayerWakeUp;
    [SerializeField] private UnityEvent onPlayerDrinkWater;
    [SerializeField] private float approachDuration = 1.5f;
    [SerializeField] private float lineDuration = 2f;
    [SerializeField] private float fadeDuration = 1.25f;
    private bool _playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForHappyEnding()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Ending"
            && FindFirstObjectByType<HappyEndingController>() == null)
            new GameObject(nameof(HappyEndingController)).AddComponent<HappyEndingController>();
    }

    private IEnumerator Start()
    {
        yield return null;
        BeginHappyEnding();
    }

    public void BeginHappyEnding()
    {
        if (_playing) return;
        _playing = true;
        StartCoroutine(PlayHappyEnding());
    }

    public IEnumerator PlayHappyEnding()
    {
        CreateFallbackPresentation();
        GameManager.Instance?.StopAllGameplay();
        onFinalDoorOpened?.Invoke();
        if (finalDoor != null) finalDoor.SetActive(true);
        SetCamera(endingCamera);
        yield return MoveFavorite();
        yield return Line("최애", "진짜 왔네.");
        yield return Line("플레이어", "늦어서 미안해.");
        yield return Line("최애", "이번에도 안 오는 줄 알았어.");
        yield return Line("플레이어", "기다려줘서 고마워.");
        yield return Line("최애", "와 줘서 고마워.");
        yield return Fade(0f, 1f);
        SetCamera(bedroomCamera);
        if (bed != null) bed.SetActive(true);
        yield return Fade(1f, 0f);
        onPlayerWakeUp?.Invoke();
        yield return Line("플레이어", "아… 씨발, 꿈이었네.");
        if (waterCup != null) waterCup.SetActive(true);
        yield return WaitForWaterDrink();
        if (happyPhoto != null) happyPhoto.SetActive(true);
        if (badPhoto != null) badPhoto.SetActive(false);
        if (phone != null) phone.SetActive(true);
        if (phoneMessageText != null) phoneMessageText.text = "오늘 몇 시에 만날 거야?";
        yield return new WaitForSecondsRealtime(2f);
        yield return Fade(0f, 1f);
        if (resultUI != null) resultUI.SetActive(true);
    }

    private IEnumerator WaitForWaterDrink()
    {
        if (waterCup == null)
        {
            onPlayerDrinkWater?.Invoke();
            yield break;
        }

        waterCupInteraction ??= waterCup.GetComponent<WaterCupInteraction>();
        if (waterCupInteraction == null)
            waterCupInteraction = waterCup.AddComponent<WaterCupInteraction>();
        if (player != null) waterCupInteraction.SetPlayer(player);

        waterCupInteraction.Used += PlayDrinkAnimation;
        yield return waterCupInteraction.WaitForDrink();
        waterCupInteraction.Used -= PlayDrinkAnimation;
    }

    private void PlayDrinkAnimation() => onPlayerDrinkWater?.Invoke();

    private IEnumerator MoveFavorite()
    {
        if (favoriteCharacter == null || favoriteApproachTarget == null) yield break;
        var start = favoriteCharacter.position;
        for (var t = 0f; t < approachDuration; t += Time.unscaledDeltaTime)
        {
            favoriteCharacter.position = Vector3.Lerp(start, favoriteApproachTarget.position, Mathf.SmoothStep(0f, 1f, t / approachDuration));
            yield return null;
        }
        favoriteCharacter.position = favoriteApproachTarget.position;
    }

    private IEnumerator Line(string speaker, string line)
    {
        if (dialogueUI != null) dialogueUI.SetActive(true);
        if (speakerText != null) speakerText.text = speaker;
        if (dialogueText != null) dialogueText.text = line;
        yield return new WaitForSecondsRealtime(lineDuration);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (brightFadeImage == null) yield break;
        brightFadeImage.gameObject.SetActive(true);
        for (var t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            brightFadeImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(from, to, t / fadeDuration));
            yield return null;
        }
        brightFadeImage.color = Color.white;
    }

    private void SetCamera(Camera active)
    {
        if (endingCamera != null) endingCamera.gameObject.SetActive(active == endingCamera);
        if (bedroomCamera != null) bedroomCamera.gameObject.SetActive(active == bedroomCamera);
    }

    // Allows the ending to play in a freshly created Ending scene while still permitting
    // authored references in the inspector to replace every part of this fallback.
    private void CreateFallbackPresentation()
    {
        if (dialogueUI != null || resultUI != null) return;

        var canvasRoot = new GameObject("HappyEndingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        dialogueUI = Panel("DialogueUI", canvasRoot.transform, new Color(0f, 0f, 0f, .72f));
        Stretch(dialogueUI.GetComponent<RectTransform>(), new Vector2(.06f, .06f), new Vector2(.94f, .22f));
        speakerText = Text("Speaker", dialogueUI.transform, 30, new Vector2(.06f, .62f), new Vector2(.94f, .94f));
        dialogueText = Text("Dialogue", dialogueUI.transform, 36, new Vector2(.06f, .08f), new Vector2(.94f, .65f));

        phone = Panel("Phone", canvasRoot.transform, new Color(.12f, .16f, .21f, .95f));
        Stretch(phone.GetComponent<RectTransform>(), new Vector2(.63f, .54f), new Vector2(.92f, .78f));
        phoneMessageText = Text("Message", phone.transform, 28, new Vector2(.08f, .1f), new Vector2(.92f, .9f));
        phone.SetActive(false);

        happyPhoto = Panel("HappyPhoto", canvasRoot.transform, new Color(.86f, .76f, .5f, 1f));
        Stretch(happyPhoto.GetComponent<RectTransform>(), new Vector2(.60f, .25f), new Vector2(.90f, .45f));
        var photoText = Text("Caption", happyPhoto.transform, 26, Vector2.zero, Vector2.one);
        photoText.text = "플레이어와 최애의 사진";
        happyPhoto.SetActive(false);
        badPhoto = new GameObject("BadPhoto");

        waterCup = Panel("WaterCup", canvasRoot.transform, new Color(.25f, .7f, .95f, .9f));
        Stretch(waterCup.GetComponent<RectTransform>(), new Vector2(.38f, .22f), new Vector2(.58f, .31f));
        var cupText = Text("Caption", waterCup.transform, 25, Vector2.zero, Vector2.one);
        cupText.text = "물컵";
        waterCup.SetActive(false);
        player = canvasRoot.transform;

        resultUI = Panel("ResultUI", canvasRoot.transform, new Color(.04f, .04f, .07f, 1f));
        Stretch(resultUI.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var resultText = Text("Result", resultUI.transform, 38, new Vector2(.1f, .22f), new Vector2(.9f, .78f));
        var result = GameResultManager.Instance?.CurrentResult ?? new GameResultData { endingType = EndingType.Happy };
        resultText.text = $"최애 구출 성공!\n\n최종 기록: {result.totalPlayTime / 60f:00}:{result.totalPlayTime % 60f:00.00}\n사망: {result.deathCount}회\n최소 남은 시간: {result.lastMapRemainingTime:0.00}초"
            + (result.isNewBest ? "\n\n새로운 최고 기록!" : string.Empty);
        resultUI.SetActive(false);

        brightFadeImage = new GameObject("FadeCanvas", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        brightFadeImage.transform.SetParent(canvasRoot.transform, false);
        Stretch(brightFadeImage.rectTransform, Vector2.zero, Vector2.one);
        brightFadeImage.color = new Color(1f, 1f, 1f, 0f);
        brightFadeImage.raycastTarget = false;
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text Text(string name, Transform parent, float size, Vector2 min, Vector2 max)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform, min, max);
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
