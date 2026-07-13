using System.Collections;
using UnityEngine;

public class HappyEndingSceneManager : MonoBehaviour
{
    [Header("Sequence References")]
    [SerializeField] private HappyEndingFadeManager fadeManager;
    [SerializeField] private HappyEndingDoor door;
    [SerializeField] private HappyEndingCameraController cameraController;
    [SerializeField] private GameObject characterRoot;
    [SerializeField] private HappyEndingDialoguePanel dialoguePanel;

    [Header("Timing")]
    [SerializeField] private float openingFadeDuration = 0.7f;
    [SerializeField] private float doorZoomDuration = 0.8f;
    [SerializeField] private float doorZoomOrthographicSize = 1.6f;
    [SerializeField] private float whiteFadeDuration = 0.45f;
    [SerializeField] private float characterRevealDuration = 0.45f;

    private bool _doorOpeningSequenceStarted;

    private void Awake()
    {
        fadeManager?.SetWhiteAlpha(1f);
        door?.SetInteractionEnabled(false);

        if (characterRoot != null)
            characterRoot.SetActive(false);

        dialoguePanel?.Hide();
    }

    private void OnEnable()
    {
        if (door != null)
            door.Opened += BeginDoorOpeningSequence;
    }

    private void Start()
    {
        StartCoroutine(BeginScene());
    }

    private void OnDisable()
    {
        if (door != null)
            door.Opened -= BeginDoorOpeningSequence;
    }

    private IEnumerator BeginScene()
    {
        if (fadeManager != null)
            yield return fadeManager.FadeFromWhite(openingFadeDuration);

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
        if (cameraController != null && door != null)
            yield return cameraController.ZoomTo(door.ZoomTarget, doorZoomOrthographicSize, doorZoomDuration);

        if (fadeManager != null)
            yield return fadeManager.FadeToWhite(whiteFadeDuration);

        door?.gameObject.SetActive(false);
        if (characterRoot != null)
            characterRoot.SetActive(true);

        if (fadeManager != null)
            yield return fadeManager.FadeFromWhite(characterRevealDuration);

        dialoguePanel?.Show();
    }
}
