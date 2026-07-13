using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 중앙 이미지로 제시된 순서를 기억한 뒤, 같은 순서의 버튼을 누르는 응급 처치 미니게임입니다.
/// imageSequence와 answerButtons의 같은 인덱스가 한 쌍입니다.
/// </summary>
public class EmergencySequenceController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image patientImage;
    [SerializeField] private Sprite[] imageSequence = new Sprite[4];
    [SerializeField] private UnityEngine.UI.Button[] answerButtons = new UnityEngine.UI.Button[4];

    [Header("Sequence Timing")]
    [SerializeField, Min(0.05f)] private float imageDisplayDuration = 0.8f;
    [SerializeField, Min(0f)] private float intervalBetweenImages = 0.2f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool hideImageWhileAnswering = true;
    [SerializeField] private bool restartOnWrongAnswer = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onSequenceShown;
    [SerializeField] private UnityEvent onCorrectAnswer;
    [SerializeField] private UnityEvent onWrongAnswer;

    public event Action SequenceShown;
    public event Action CorrectAnswer;
    public event Action WrongAnswer;

    private Coroutine sequenceRoutine;
    private int nextAnswerIndex;
    private bool acceptingInput;

    private void Awake()
    {
        BindButtons();
        SetButtonsInteractable(false);
    }

    private void OnEnable()
    {
        if (playOnEnable)
            StartSequence();
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = null;
        acceptingInput = false;
        SetButtonsInteractable(false);
    }

    /// <summary>이미지 4개를 처음부터 다시 보여 줍니다.</summary>
    public void StartSequence()
    {
        if (!HasValidSetup())
            return;

        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        acceptingInput = false;
        nextAnswerIndex = 0;
        SetButtonsInteractable(false);
        patientImage.gameObject.SetActive(true);

        for (int i = 0; i < imageSequence.Length; i++)
        {
            patientImage.sprite = imageSequence[i];
            yield return new WaitForSeconds(imageDisplayDuration);

            if (intervalBetweenImages > 0f && i < imageSequence.Length - 1)
                yield return new WaitForSeconds(intervalBetweenImages);
        }

        if (hideImageWhileAnswering)
            patientImage.gameObject.SetActive(false);

        acceptingInput = true;
        SetButtonsInteractable(true);
        sequenceRoutine = null;
        onSequenceShown?.Invoke();
        SequenceShown?.Invoke();
    }

    private void BindButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null)
                continue;

            int buttonIndex = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(buttonIndex));
        }
    }

    private void OnAnswerButtonClicked(int clickedIndex)
    {
        if (!acceptingInput)
            return;

        if (clickedIndex != nextAnswerIndex)
        {
            acceptingInput = false;
            SetButtonsInteractable(false);
            onWrongAnswer?.Invoke();
            WrongAnswer?.Invoke();

            if (restartOnWrongAnswer)
                StartSequence();

            return;
        }

        nextAnswerIndex++;
        if (nextAnswerIndex < answerButtons.Length)
            return;

        acceptingInput = false;
        SetButtonsInteractable(false);
        onCorrectAnswer?.Invoke();
        CorrectAnswer?.Invoke();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (answerButtons == null)
            return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
                answerButtons[i].interactable = interactable;
        }
    }

    private bool HasValidSetup()
    {
        if (patientImage == null || imageSequence == null || answerButtons == null ||
            imageSequence.Length != 4 || answerButtons.Length != 4)
        {
            Debug.LogError("EmergencySequenceController: 중앙 Image와 이미지/버튼 4개를 연결하세요.", this);
            return false;
        }

        for (int i = 0; i < imageSequence.Length; i++)
        {
            if (imageSequence[i] == null || answerButtons[i] == null)
            {
                Debug.LogError($"EmergencySequenceController: {i + 1}번 이미지 또는 버튼이 비어 있습니다.", this);
                return false;
            }
        }

        return true;
    }
}
