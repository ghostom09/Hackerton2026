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
    [SerializeField] private CanvasGroup dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Ending")]
    [SerializeField] private GameObject endingImage;
    [SerializeField] private CanvasGroup whiteFadePanel;

    [Header("Ending Camera Background")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color endingCameraBackgroundColor = Color.white;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogues;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 2f;
    [SerializeField] private float dialogueDelay = 0.5f;
    [SerializeField] private float dialogueFadeDuration = 0.5f;
    [SerializeField] private float endingImageDuration = 3f;
    [SerializeField] private float endingFadeDuration = 2f;

    private readonly Color activeColor = Color.white;
    private readonly Color inactiveColor = new(0.35f, 0.35f, 0.35f, 1f);

    private Stage currentStage = Stage.Starting;
    private int dialogueIndex;
    private bool changingStage;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackgroundColor;
    private bool cameraStateSaved;

    private void Awake()
    {
        Time.timeScale = 1f;

        EnsureDefaultDialogues();
        SaveCameraState();

        SetCanvasGroup(dialoguePanel, false, 0f);
        SetCanvasGroup(whiteFadePanel, true, 1f);
        SetCharactersActive(false);

        if (endingImage != null)
            endingImage.SetActive(false);
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

        yield return Fade(whiteFadePanel, 1f, 0f, openingFadeDuration);

        if (whiteFadePanel != null)
            whiteFadePanel.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(dialogueDelay);

        SetCharactersActive(true);
        yield return Fade(dialoguePanel, 0f, 1f, dialogueFadeDuration);

        changingStage = false;
        currentStage = Stage.Dialogue;
        dialogueIndex = 0;

        if (dialogues == null || dialogues.Length == 0)
        {
            StartCoroutine(PlayEndingImage());
            yield break;
        }

        ShowDialogue(dialogues[dialogueIndex]);
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

        SetCanvasGroup(dialoguePanel, false, 0f);
        SetCharactersActive(false);
        SetEndingCameraBackground();

        if (endingImage != null)
            endingImage.SetActive(true);

        yield return new WaitForSecondsRealtime(endingImageDuration);
        yield return Fade(whiteFadePanel, 0f, 1f, endingFadeDuration);

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

    private void SaveCameraState()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        originalCameraClearFlags = targetCamera.clearFlags;
        originalCameraBackgroundColor = targetCamera.backgroundColor;
        cameraStateSaved = true;
    }

    private void SetEndingCameraBackground()
    {
        if (targetCamera == null)
            return;

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = endingCameraBackgroundColor;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateSaved || targetCamera == null)
            return;

        targetCamera.clearFlags = originalCameraClearFlags;
        targetCamera.backgroundColor = originalCameraBackgroundColor;
    }

    private void OnDestroy()
    {
        RestoreCameraState();
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
