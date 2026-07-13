using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Plays the rescue-failure sequence in the BadEnding scene.</summary>
public sealed class BadEndingController : MonoBehaviour
{
    [Header("Optional authored scene objects")]
    [SerializeField] private GameObject player, phone, bed, waterCup, emptyPhotoFrame, favoriteOnlyPhoto, television, happyPhoto, badPhoto;
    [SerializeField] private WaterCupInteraction waterCupInteraction;
    [SerializeField] private Camera bedroomCamera;
    [SerializeField] private AudioSource phoneAudioSource, newsAudioSource;
    [SerializeField] private AudioClip phoneNotificationClip, connectionLostClip, newsClip;
    [SerializeField] private Image fadeCanvas;
    [SerializeField] private GameObject dialogueUI, resultUI;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private BadEndingPhoneChat phoneChat;
    [SerializeField] private UnityEvent onPlayerWakeUp;
    [SerializeField] private UnityEvent onPlayerDrinkWater;
    [SerializeField] private float messageInterval = 1.4f;
    [SerializeField] private float fadeDuration = 1.5f;

    private readonly string[] _lastMessages = { "계속 기다렸는데.", "이번에도 안 오는구나.", "이제 더는 못 기다리겠어." };
    private GameObject _bedroomRoot;
    private RectTransform _roomCameraTarget;
    private bool _playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForBadEnding()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BadEnding"
            && FindFirstObjectByType<BadEndingController>() == null)
            new GameObject(nameof(BadEndingController)).AddComponent<BadEndingController>();
    }

    private IEnumerator Start() { yield return null; BeginBadEnding(); }
    public void BeginBadEnding() { if (!_playing) { _playing = true; StartCoroutine(PlayBadEnding()); } }

    public IEnumerator PlayBadEnding()
    {
        StopAllGameplay();
        CreateFallbackPresentation();
        phoneChat.ShowPhone();
        PlayNotification(phoneNotificationClip, 880f);
        yield return new WaitForSecondsRealtime(.7f);
        foreach (var message in _lastMessages)
        {
            phoneChat.AddIncomingMessage(message);
            yield return new WaitForSecondsRealtime(messageInterval);
        }

        phoneChat.ShowDisconnected();
        PlayNotification(connectionLostClip, 250f);
        yield return new WaitForSecondsRealtime(1.25f);
        yield return Fade(0f, 1f);
        phoneChat.HidePhone();
        _bedroomRoot.SetActive(true);
        SetActive(bed, true); SetActive(player, true); SetCamera(bedroomCamera);
        yield return Fade(1f, 0f);
        onPlayerWakeUp?.Invoke();
        yield return Say("플레이어가 침대에서 몸을 일으킨다.", 1.2f);
        yield return Say("플레이어: 아… 씨발, 꿈이었네.", 2.2f);
        SetActive(waterCup, true);
        yield return WaitForWaterDrink();
        SetActive(emptyPhotoFrame, true); SetActive(favoriteOnlyPhoto, false); SetActive(television, true);
        yield return MoveRoomCamera();
        yield return Say("탁자 위 액자에는 함께 찍은 사진이 없다.", 2.2f);

        // The player sees the missing photo before the news explains its meaning.
        yield return Fade(0f, .72f);
        Play(newsAudioSource, newsClip);
        yield return Say("어젯밤 발생한 사고로 한 명이 숨진 채 발견됐습니다.\n\n경찰은 정확한 사고 경위를 조사하고 있습니다.", 4f);
        yield return Fade(.72f, 1f);
        SetActive(happyPhoto, false);
        SetActive(badPhoto, true);
        fadeCanvas.gameObject.SetActive(false);
        resultUI.SetActive(true);
        Time.timeScale = 1f;
    }

    public void StopAllGameplay()
    {
        GameManager.Instance?.StopAllGameplay();
        Time.timeScale = 0f;
    }

    private IEnumerator WaitForWaterDrink()
    {
        if (waterCup == null) yield break;
        waterCupInteraction ??= waterCup.GetComponent<WaterCupInteraction>();
        if (waterCupInteraction == null) waterCupInteraction = waterCup.AddComponent<WaterCupInteraction>();
        if (player != null) waterCupInteraction.SetPlayer(player.transform);
        waterCupInteraction.Used += PlayDrinkAnimation;
        yield return waterCupInteraction.WaitForDrink();
        waterCupInteraction.Used -= PlayDrinkAnimation;
    }

    private void PlayDrinkAnimation() => onPlayerDrinkWater?.Invoke();

    private IEnumerator MoveRoomCamera()
    {
        if (_roomCameraTarget == null) yield break;
        var start = _roomCameraTarget.anchoredPosition;
        var end = start + new Vector2(-180f, 0f);
        for (var t = 0f; t < 1.25f; t += Time.unscaledDeltaTime)
        {
            _roomCameraTarget.anchoredPosition = Vector2.Lerp(start, end, t / 1.25f);
            yield return null;
        }
        _roomCameraTarget.anchoredPosition = end;
    }

    private IEnumerator Say(string line, float duration)
    {
        dialogueUI.SetActive(true);
        dialogueText.text = line;
        yield return new WaitForSecondsRealtime(duration);
        dialogueUI.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeCanvas.gameObject.SetActive(true);
        for (var t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            fadeCanvas.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        fadeCanvas.color = new Color(0, 0, 0, to);
    }

    private void CreateFallbackPresentation()
    {
        phoneChat ??= FindFirstObjectByType<BadEndingPhoneChat>();
        if (phoneChat == null) phoneChat = new GameObject(nameof(BadEndingPhoneChat)).AddComponent<BadEndingPhoneChat>();
        if (bedroomCamera == null)
        {
            bedroomCamera = Camera.main;
            if (bedroomCamera != null) bedroomCamera.gameObject.name = "BedroomCamera";
        }
        phoneAudioSource ??= new GameObject("PhoneAudioSource").AddComponent<AudioSource>();
        newsAudioSource ??= new GameObject("NewsAudioSource").AddComponent<AudioSource>();
        var canvas = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var uiCanvas = canvas.GetComponent<Canvas>(); uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay; uiCanvas.sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920);

        _bedroomRoot = Panel("Bedroom", canvas.transform, new Color(.12f, .14f, .19f));
        Stretch(_bedroomRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        _roomCameraTarget = _bedroomRoot.GetComponent<RectTransform>(); _bedroomRoot.SetActive(false);
        player = Label("Player", _bedroomRoot.transform, "플레이어", 44, Color.white, new Vector2(.18f, .35f), new Vector2(.5f, .55f));
        bed = Label("Bed", _bedroomRoot.transform, "침대", 40, new Color(.7f, .8f, .94f), new Vector2(.08f, .12f), new Vector2(.55f, .32f));
        waterCup = Label("WaterCup", _bedroomRoot.transform, "물컵", 32, new Color(.65f, .9f, 1f), new Vector2(.58f, .2f), new Vector2(.76f, .3f));
        emptyPhotoFrame = Label("EmptyPhotoFrame", _bedroomRoot.transform, "사진 없음", 36, new Color(.8f, .72f, .55f), new Vector2(.63f, .48f), new Vector2(.94f, .66f));
        favoriteOnlyPhoto = Label("FavoriteOnlyPhoto", _bedroomRoot.transform, "최애의 사진", 36, Color.white, new Vector2(.63f, .48f), new Vector2(.94f, .66f));
        television = Label("Television", _bedroomRoot.transform, "TV", 34, new Color(.28f, .33f, .4f), new Vector2(.68f, .7f), new Vector2(.93f, .82f));
        SetActive(waterCup, false); SetActive(emptyPhotoFrame, false); SetActive(favoriteOnlyPhoto, false); SetActive(television, false);

        dialogueUI = Panel("DialogueUI", canvas.transform, new Color(0, 0, 0, .74f));
        Stretch(dialogueUI.GetComponent<RectTransform>(), new Vector2(.06f, .06f), new Vector2(.94f, .19f));
        dialogueText = dialogueUI.AddComponent<TextMeshProUGUI>(); dialogueText.fontSize = 32; dialogueText.color = Color.white; dialogueText.alignment = TextAlignmentOptions.Center;
        Stretch(dialogueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22, 12), new Vector2(-22, -12)); dialogueUI.SetActive(false);

        resultUI = Panel("ResultUI", canvas.transform, new Color(.04f, .04f, .06f)); Stretch(resultUI.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var result = GameResultManager.Instance?.CurrentResult ?? new GameResultData { endingType = EndingType.Bad };
        var zone = Mathf.Max(1, result.lastMapIndex + 1);
        var record = $"구조 실패\n\n제한 시간 안에 도착하지 못했습니다.\n\n실패한 구역: 제{zone}구역\n도달 기록: {result.totalPlayTime / 60f:00}:{result.totalPlayTime % 60f:00.00}\n사망: {result.deathCount}회\n남은 시간: 0초\n\n조금만 더 빨랐다면…";
        var resultText = resultUI.AddComponent<TextMeshProUGUI>(); resultText.text = record; resultText.fontSize = 37; resultText.color = Color.white; resultText.alignment = TextAlignmentOptions.Center;
        Stretch(resultText.rectTransform, new Vector2(.08f, .16f), new Vector2(.92f, .84f)); resultUI.SetActive(false);
        happyPhoto = new GameObject("HappyPhoto"); happyPhoto.transform.SetParent(resultUI.transform, false);
        badPhoto = Label("BadPhoto", resultUI.transform, "", 1, Color.clear, Vector2.zero, Vector2.zero); SetActive(badPhoto, false);

        fadeCanvas = Image("Fade", canvas.transform, Color.black); Stretch(fadeCanvas.rectTransform, Vector2.zero, Vector2.one); fadeCanvas.raycastTarget = false; fadeCanvas.color = new Color(0, 0, 0, 0);
    }

    private static GameObject Panel(string name, Transform parent, Color color) { var o = new GameObject(name, typeof(RectTransform), typeof(Image)); o.transform.SetParent(parent, false); o.GetComponent<Image>().color = color; return o; }
    private static GameObject Label(string name, Transform parent, string text, float size, Color color, Vector2 min, Vector2 max) { var o = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TextMeshProUGUI)); o.transform.SetParent(parent, false); o.GetComponent<Image>().color = new Color(color.r, color.g, color.b, .35f); var t = o.GetComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center; Stretch(t.rectTransform, min, max); return o; }
    private static Image Image(string name, Transform parent, Color color) { var i = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); i.transform.SetParent(parent, false); i.color = color; return i; }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2? offsetMin = null, Vector2? offsetMax = null) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin ?? Vector2.zero; rect.offsetMax = offsetMax ?? Vector2.zero; }
    private static void SetActive(GameObject target, bool active) { if (target != null) target.SetActive(active); }
    private static void Play(AudioSource source, AudioClip clip) { if (source != null && clip != null) source.PlayOneShot(clip); }
    private void PlayNotification(AudioClip clip, float frequency)
    {
        if (clip != null) { Play(phoneAudioSource, clip); return; }
        var sampleRate = 44100;
        var length = Mathf.CeilToInt(sampleRate * .14f);
        var tone = AudioClip.Create("GeneratedPhoneTone", length, 1, sampleRate, false);
        var samples = new float[length];
        for (var i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * .12f;
        tone.SetData(samples, 0);
        phoneAudioSource.PlayOneShot(tone);
    }
    private static void SetCamera(Camera camera) { if (camera != null) camera.gameObject.SetActive(true); }
}
