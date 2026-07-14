using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HappyEndingController : MonoBehaviour
{
    public enum SpeakerSide
    {
        Left,
        Right,
        None
    }

    [Serializable]
    public class DialogueLine
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string dialogue;

        public SpeakerSide speakerSide;
    }

    private enum Stage
    {
        Starting,
        Dialogue,
        EndingImage,
        Finished
    }

    [Header("Characters")]
    [SerializeField] private Image leftCharacter;
    [SerializeField] private Image rightCharacter;

    [Header("Dialogue UI")]
    [SerializeField] private Image dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("Optional background sprite shown only while the dialogue panel is visible.")]
    [SerializeField] private GameObject dialogueBackground;

    [Header("Ending")]
    [SerializeField] private GameObject endingImage;
    [SerializeField] private Image whiteFadePanel;
    [SerializeField] private TMP_Text thankYouText;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogues;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 2f;
    [SerializeField] private float dialogueDelay = 0.5f;
    [SerializeField] private float dialogueFadeDuration = 0.5f;
    [SerializeField] private float endingImageDuration = .2f;
    [SerializeField] private float endingFadeDuration = 2f;
    [SerializeField] private float endingImageFadeInDuration = .25f;
    [SerializeField] private float endingImageFadeOutDuration = .35f;
    [SerializeField] private float endingImageRiseDuration = 1.1f;
    [SerializeField] private float thankYouDuration = .75f;

    private readonly Color activeColor = Color.white;
    private readonly Color inactiveColor = new(0.35f, 0.35f, 0.35f, 1f);

    private Stage currentStage = Stage.Starting;
    private int dialogueIndex;
    private bool changingStage;
    private CanvasGroup _thankYouGroup;
    private Vector2 _endingImagePosition;
    private void Awake()
    {
        Time.timeScale = 1f;

        if (whiteFadePanel == null)
            whiteFadePanel = GameObject.Find("WhiteFadePanel")?.GetComponent<Image>();
        if (dialoguePanel == null)
            dialoguePanel = GameObject.Find("DialoguePanel")?.GetComponent<Image>();

        EnsureDefaultDialogues();

        SetImageVisible(dialoguePanel, false, 0f);
        SetDialogueBackgroundVisible(false);
        SetImageVisible(whiteFadePanel, true, 1f);
        SetCharactersActive(false);

        if (endingImage != null)
        {
            if (endingImage.transform is RectTransform endingRect)
                _endingImagePosition = endingRect.anchoredPosition;
            endingImage.SetActive(false);
        }

        CreateThankYouTextIfNeeded();
    }

    private void Start()
    {
        StartCoroutine(StartHappyEnding());
    }

    private void Update()
    {
        if (changingStage || currentStage != Stage.Dialogue)
            return;

        bool pressSpace = Keyboard.current != null &&
                          Keyboard.current.spaceKey.wasPressedThisFrame;

        bool mouseClick = Mouse.current != null &&
                          Mouse.current.leftButton.wasPressedThisFrame;

        if (pressSpace || mouseClick)
            NextDialogue();
    }

    private IEnumerator StartHappyEnding()
    {
        changingStage = true;

        // Prepare the first dialogue behind the white screen. The opening fade-out
        // then reveals a fully loaded conversation instead of an empty scene.
        dialogueIndex = 0;
        bool hasDialogue = dialogues != null && dialogues.Length > 0;
        if (hasDialogue)
        {
            SetCharactersActive(true);
            SetImageVisible(dialoguePanel, true, 1f);
            SetDialogueBackgroundVisible(true);
            ShowDialogue(dialogues[dialogueIndex]);
        }

        yield return new WaitForSecondsRealtime(dialogueDelay);
        yield return FadeImage(whiteFadePanel, 1f, 0f, openingFadeDuration);

        if (whiteFadePanel != null)
            whiteFadePanel.gameObject.SetActive(false);

        changingStage = false;
        currentStage = Stage.Dialogue;

        if (!hasDialogue)
        {
            StartCoroutine(PlayEndingImage());
            yield break;
        }
    }

    private void NextDialogue()
    {
        dialogueIndex++;

        if (dialogueIndex >= dialogues.Length)
        {
            StartCoroutine(PlayEndingImage());
            return;
        }

        ShowDialogue(dialogues[dialogueIndex]);
    }

    private void ShowDialogue(DialogueLine line)
    {
        if (line == null)
            return;

        if (nameText != null)
            nameText.text = line.speakerName;

        if (dialogueText != null)
            dialogueText.text = line.dialogue;

        switch (line.speakerSide)
        {
            case SpeakerSide.Left:
                SetCharacterColors(activeColor, inactiveColor);
                break;

            case SpeakerSide.Right:
                SetCharacterColors(inactiveColor, activeColor);
                break;

            case SpeakerSide.None:
                SetCharacterColors(inactiveColor, inactiveColor);
                break;
        }
    }

    private IEnumerator PlayEndingImage()
    {
        if (changingStage)
            yield break;

        changingStage = true;
        currentStage = Stage.EndingImage;

        SetImageVisible(dialoguePanel, false, 0f);
        SetDialogueBackgroundVisible(false);
        SetCharactersActive(false);

        // Fade to white first, then place the ending image behind the white overlay.
        yield return FadeImage(whiteFadePanel, 0f, 1f, endingImageFadeInDuration);

        if (endingImage != null)
        {
            endingImage.SetActive(true);
            yield return RiseEndingImage();
        }

        // The fade-out reveals the image only after it has appeared in place.
        yield return FadeImage(whiteFadePanel, 1f, 0f, endingImageFadeOutDuration);

        yield return new WaitForSecondsRealtime(endingImageDuration);
        yield return ShowThankYou();

        // Final white fade before returning to the title screen.
        yield return FadeImage(whiteFadePanel, 0f, 1f, endingFadeDuration);

        currentStage = Stage.Finished;
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

    private IEnumerator RiseEndingImage()
    {
        if (endingImage == null || endingImage.transform is not RectTransform imageRect)
            yield break;

        var startPosition = _endingImagePosition + Vector2.down * 760f;
        float safeDuration = Mathf.Max(.01f, endingImageRiseDuration);
        for (float elapsed = 0f; elapsed < safeDuration; elapsed += Time.unscaledDeltaTime)
        {
            imageRect.anchoredPosition = Vector2.Lerp(startPosition, _endingImagePosition, elapsed / safeDuration);
            yield return null;
        }
        imageRect.anchoredPosition = _endingImagePosition;
    }

    private IEnumerator ShowThankYou()
    {
        if (_thankYouGroup == null)
            yield break;

        yield return Fade(_thankYouGroup, 0f, 1f, .45f);
        yield return new WaitForSecondsRealtime(thankYouDuration);
        yield return Fade(_thankYouGroup, 1f, 0f, .45f);
    }

    private void CreateThankYouTextIfNeeded()
    {
        if (thankYouText == null && whiteFadePanel != null && whiteFadePanel.transform.parent != null)
        {
            var textObject = new GameObject("ThankYouText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            textObject.transform.SetParent(whiteFadePanel.transform.parent, false);
            thankYouText = textObject.GetComponent<TextMeshProUGUI>();
            var rect = thankYouText.rectTransform;
            rect.anchorMin = new Vector2(.2f, .42f);
            rect.anchorMax = new Vector2(.8f, .58f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            thankYouText.font = TMP_Settings.defaultFontAsset;
            thankYouText.text = "\uACE0\uB9C8\uC6CC";
            thankYouText.fontSize = 72f;
            thankYouText.alignment = TextAlignmentOptions.Center;
            thankYouText.color = Color.white;
            _thankYouGroup = textObject.GetComponent<CanvasGroup>();
        }
        else if (thankYouText != null)
        {
            _thankYouGroup = thankYouText.GetComponent<CanvasGroup>();
            if (_thankYouGroup == null)
                _thankYouGroup = thankYouText.gameObject.AddComponent<CanvasGroup>();
        }

        if (_thankYouGroup != null)
            SetCanvasGroup(_thankYouGroup, false, 0f);
    }

    private static IEnumerator Fade(
        CanvasGroup target,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (target == null)
            yield break;

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

    private static IEnumerator FadeImage(Image target, float startAlpha, float endAlpha, float duration)
    {
        if (target == null)
            yield break;

        target.gameObject.SetActive(true);
        var color = target.color;
        color.a = startAlpha;
        target.color = color;
        target.raycastTarget = true;

        float safeDuration = Mathf.Max(.01f, duration);
        for (float elapsed = 0f; elapsed < safeDuration; elapsed += Time.unscaledDeltaTime)
        {
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / safeDuration);
            target.color = color;
            yield return null;
        }

        color.a = endAlpha;
        target.color = color;
        target.raycastTarget = endAlpha > 0f;
    }

    private static void SetImageVisible(Image target, bool visible, float alpha)
    {
        if (target == null)
            return;

        target.gameObject.SetActive(visible);
        var color = target.color;
        color.a = alpha;
        target.color = color;
        target.raycastTarget = visible && alpha > 0f;
    }

    private void SetCharactersActive(bool active)
    {
        if (leftCharacter != null)
            leftCharacter.gameObject.SetActive(active);

        if (rightCharacter != null)
            rightCharacter.gameObject.SetActive(active);
    }

    private void SetCharacterColors(Color leftColor, Color rightColor)
    {
        if (leftCharacter != null)
            leftCharacter.color = leftColor;

        if (rightCharacter != null)
            rightCharacter.color = rightColor;
    }

    private void SetDialogueBackgroundVisible(bool visible)
    {
        if (dialogueBackground != null)
            dialogueBackground.SetActive(visible);
    }

    private void EnsureDefaultDialogues()
    {
        if (dialogues != null && dialogues.Length > 0)
            return;

        dialogues = new[]
        {
            Yuna("아, 드디어 내게 와주었구나 내 사랑."),
            Yuna("날 바라봐주고, 날 믿어주고, 날 위해주고, 종국에는 날 죽여줄 그런 너가 왔구나."),
            Yuna("사랑해."),
            Yuna("너도 그렇지? 여기까지 왔다면 더 이상 갈 곳은 없는 거잖아."),
            Yuna("그래. 너도 날 위해서, 이런 곳까지 왔구나."),
            Yuna("걱정마. 이 곳에는 너랑 나뿐이야. 다른 사람따위 없어. 넌 나만 바라보면 돼, 나도 너만 바라보면 되고."),
            Yuna("너가 원한다면, 난 너가 원하는 것들을 해줄 수 있어. 여기서 나가는 것 말고 전부."),
            Yuna("그래, 그 표정을 보니 너도 원했던거구나. 역시 내 사랑이야. 널 항상 믿고있는 내 덕분이겠지."),
            Yuna("그렇게 날 위해 더 헌신해주고 위해주고 날 위해 죽어줘, 너랑 난 떨어질 수 없는 관계잖아."),
            Yuna("내가 죽고, 너도 죽고. 하데스와 페르세포네같이, 우린 여기서 같이 사는거야."),
            Yuna("넌 퍼즐을 풀었고, 난 여기에 있으니. 더 이상 우린 멀어질 수 없어."),
            Yuna("아아, 그래 날 안아줘. 내가 널 사랑한다면, 너가 날 사랑한다면. 나에게 와서 날 안아줘"),
            Yuna("그래, 좀 더 세게 안아줘, 내가 너 말고 모든 것을 잊게. 모든 말을 잊고, 모든 행동을 잊고, 모든 생각을 잊을 수 있게. 너만 생각할 수 있게 해줘."),
            Yuna("더, 더 세게 안아줘. 내가 너 안에서 터져 죽을 수 있게. 너가 영원히 날 죽였다는 죄책감 안에서. 날 생각하며 살아갈 수 있게."),
            Yuna("아아, 넌 내가 죽고 얼마 안가서 죽겠지. 하지만 그 동안은 내 생각밖에 못하게 될거야."),
            Yuna("너의 사랑은 중독적이고ㅡ 또, 위험하니까."),
            Yuna("내가 널 가지고, 너도 날 가지고. 우리 같이 뒤섞여서 누가 나임을 잊을 수 있게 더 세게 안아줘."),
            Yuna("너가 날 사랑한다면 그정돈 해줄 수 있지?"),
            Yuna("그래. 그 정돈 해줄 수 있을거야."),
            Yuna("넌 마음이 약해서 날 터져죽게 안아주진 못하겠지만, 넌 마음이 약해서 이런 말을 한 영원히 기억하겠지."),
            Yuna("날 안아줘."),
            Yuna("날 너의 품에서 죽게해줘."),
            Yuna("날… 너 안에서 살아가게 해줘."),
            Yuna("그래, 너의 선택은 그렇구나."),
            Yuna("…나도 사랑해.")
        };
    }

    private static DialogueLine Yuna(string dialogue)
    {
        return new DialogueLine
        {
            speakerName = "유나",
            dialogue = dialogue,
            speakerSide = SpeakerSide.Right
        };
    }

    private static void SetCanvasGroup(CanvasGroup group, bool active, float alpha)
    {
        if (group == null)
            return;

        group.gameObject.SetActive(active);
        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;
    }
}
