using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>이미지 표시와 버튼 입력을 연결하고 성공/실패를 알립니다.</summary>
public class EmergencySequenceController : MonoBehaviour
{
    [SerializeField] private EmergencyImageSequence imageSequence;
    [SerializeField] private EmergencyButtonSequence buttonSequence;
    [SerializeField] private Timer timer;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool restartOnWrongAnswer = true;
    [SerializeField, Min(0f)] private float wrongAnswerTimePenalty = 5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSequenceShown;
    [SerializeField] private UnityEvent onCorrectAnswer;
    [SerializeField] private UnityEvent onWrongAnswer;

    public event Action SequenceShown;
    public event Action CorrectAnswer;
    public event Action WrongAnswer;

    private void Awake()
    {
        if (timer == null)
            timer = FindFirstObjectByType<Timer>();

        if (imageSequence != null)
            imageSequence.Shown += OnSequenceShown;

        if (buttonSequence != null)
        {
            buttonSequence.Completed += OnCorrectAnswer;
            buttonSequence.WrongAnswer += OnWrongAnswer;
        }
    }

    private void OnDestroy()
    {
        if (imageSequence != null)
            imageSequence.Shown -= OnSequenceShown;

        if (buttonSequence != null)
        {
            buttonSequence.Completed -= OnCorrectAnswer;
            buttonSequence.WrongAnswer -= OnWrongAnswer;
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
            StartSequence();
    }

    public void StartSequence()
    {
        if (imageSequence == null || buttonSequence == null)
        {
            Debug.LogError("EmergencySequenceController: Image Sequence와 Button Sequence를 연결하세요.", this);
            return;
        }

        buttonSequence.EndInput();
        imageSequence.Play();
    }

    private void OnSequenceShown()
    {
        buttonSequence.BeginInput(imageSequence.DisplaySequence);
        onSequenceShown?.Invoke();
        SequenceShown?.Invoke();
    }

    private void OnCorrectAnswer()
    {
        onCorrectAnswer?.Invoke();
        CorrectAnswer?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.RequestNextMap();
    }

    private void OnWrongAnswer()
    {
        if (timer != null)
            timer.ReduceTime(wrongAnswerTimePenalty);

        onWrongAnswer?.Invoke();
        WrongAnswer?.Invoke();

        if (restartOnWrongAnswer)
            StartSequence();
    }
}
