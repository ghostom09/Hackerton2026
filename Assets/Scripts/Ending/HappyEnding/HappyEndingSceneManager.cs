using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HappyEndingSceneManager : MonoBehaviour
{
    [Header("Sequence References")]
    [SerializeField] private HappyEndingFadeManager fadeManager;
    [SerializeField] private HappyEndingDoor door;
    [SerializeField] private HappyEndingCameraController cameraController;
    [SerializeField] private GameObject characterRoot;
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite smilingCharacterSprite;
    [SerializeField] private HappyEndingDialoguePanel dialoguePanel;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 1.6f;
    [SerializeField] private float doorOpenDelay = 0.75f;
    [SerializeField] private float doorZoomDuration = 1.4f;
    [SerializeField] private float doorZoomOrthographicSize = 1.6f;
    [SerializeField] private float whiteHoldDuration = 0.8f;
    [SerializeField] private float characterRevealDuration = 1f;
    [SerializeField] private float endingWhiteFadeDuration = 1.2f;
    [SerializeField] private float achievementDuration = 3f;

    private bool _doorOpeningSequenceStarted;
    private bool _waitingForEndingClick;
    private bool _endingSequenceStarted;
    private int _endingReadyFrame;

    // Keep the opening transition setup in one place for scene reloads.
    private void Awake()
    {
        if (door == null)
            door = FindFirstObjectByType<HappyEndingDoor>();

        fadeManager?.SetBlackAlpha(1f);
        door?.SetInteractionEnabled(false);

        if (characterRoot != null)
            characterRoot.SetActive(false);

        dialoguePanel?.Hide();
    }

    private void OnEnable()
    {
        if (door != null)
            door.Opened += BeginDoorOpeningSequence;
        if (dialoguePanel != null)
            dialoguePanel.DialogueCompleted += WaitForEndingClick;
    }

    private void Start()
    {
        StartCoroutine(BeginScene());
    }

    private void OnDisable()
    {
        if (door != null)
            door.Opened -= BeginDoorOpeningSequence;
        if (dialoguePanel != null)
            dialoguePanel.DialogueCompleted -= WaitForEndingClick;
    }

    private void Update()
    {
        if (!_waitingForEndingClick || _endingSequenceStarted || Time.frameCount <= _endingReadyFrame)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            StartCoroutine(FinishHappyEnding());
    }

    private IEnumerator BeginScene()
    {
        if (fadeManager != null)
            yield return fadeManager.FadeFromBlack(openingFadeDuration);

        door?.SetInteractionEnabled(true);
    }

    private void BeginDoorOpeningSequence()
    {
        if (_doorOpeningSequenceStarted)
            return;

        _doorOpeningSequenceStarted = true;
        StartCoroutine(EnterDoor());
    }

    private IEnumerator EnterDoor()
    {
        yield return new WaitForSecondsRealtime(doorOpenDelay);
        door?.HideHandle();

        Coroutine whiteFade = null;
        if (fadeManager != null)
            whiteFade = fadeManager.FadeToWhite(doorZoomDuration);

        if (cameraController != null && door != null)
            yield return cameraController.ZoomTo(door.ZoomTarget, doorZoomOrthographicSize, doorZoomDuration);
        else
            yield return new WaitForSecondsRealtime(doorZoomDuration);

        if (whiteFade != null)
            yield return whiteFade;

        yield return new WaitForSecondsRealtime(whiteHoldDuration);

        door?.gameObject.SetActive(false);
        if (characterRoot != null)
            characterRoot.SetActive(true);

        if (characterImage != null && smilingCharacterSprite != null)
            characterImage.sprite = smilingCharacterSprite;

        if (fadeManager != null)
            yield return fadeManager.FadeFromWhite(characterRevealDuration);

        dialoguePanel?.Show();
    }

    private void WaitForEndingClick()
    {
        _waitingForEndingClick = true;
        _endingReadyFrame = Time.frameCount;
    }

    private IEnumerator FinishHappyEnding()
    {
        _endingSequenceStarted = true;
        _waitingForEndingClick = false;

        if (fadeManager != null)
            yield return fadeManager.FadeToWhite(endingWhiteFadeDuration);

        yield return new WaitForSecondsRealtime(achievementDuration);

        if (SceneManager.Instance != null)
            SceneManager.Instance.ChangeScene(SceneName.MainMenu);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
