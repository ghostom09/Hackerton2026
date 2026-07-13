using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MathQuizController : MonoBehaviour
{
    [Header("Question Data")]
    [SerializeField] private MathProblemSO[] problems;
    [SerializeField] private bool shuffleQuestions;

    [Header("Scene References")]
    [SerializeField] private MathQuizUI quizUI;
    [SerializeField] private MathQuizInputHandler inputHandler;

    public event Action<MathProblemSO> QuestionShown;
    public event Action<MathProblemSO> CorrectAnswerSubmitted;
    public event Action<MathProblemSO> WrongAnswerSubmitted;
    public event Action QuizCompleted;

    private readonly List<MathProblemSO> _questionOrder = new();
    private int _currentQuestionIndex;
    private string[] _currentChoices;
    private int _correctAnswerCount;
    private bool _quizCompleted;

    private MathProblemSO CurrentProblem => HasCurrentProblem ? _questionOrder[_currentQuestionIndex] : null;
    private int CurrentQuestionNumber => _questionOrder.Count == 0 ? 0 : _currentQuestionIndex + 1;
    private int QuestionCount => _questionOrder.Count;
    public bool IsCompleted => _quizCompleted;

    private bool HasCurrentProblem => _currentQuestionIndex >= 0 && _currentQuestionIndex < _questionOrder.Count;

    private void Awake()
    {
        if (quizUI == null)
            quizUI = GetComponent<MathQuizUI>();

        if (inputHandler == null)
            inputHandler = GetComponent<MathQuizInputHandler>();
    }

    private void Start()
    {
        if (quizUI == null || inputHandler == null)
        {
            Debug.LogError("MathQuizUI or MathQuizInputHandler is missing.", this);
            return;
        }

        if (!quizUI.IsReady)
        {
            Debug.LogError("MathQuizUI references are not assigned. Connect the manually created UI objects in the Inspector.", this);
            return;
        }

        inputHandler.Bind(this, quizUI);
        StartQuiz();
    }

    private void StartQuiz()
    {
        _questionOrder.Clear();

        if (problems != null)
        {
            foreach (var problem in problems)
            {
                if (problem != null)
                    _questionOrder.Add(problem);
            }
        }

        if (shuffleQuestions)
            Shuffle(_questionOrder);

        _currentQuestionIndex = 0;
        _correctAnswerCount = 0;
        _quizCompleted = false;

        if (_questionOrder.Count == 0)
        {
            quizUI.ShowConfigurationError();
            return;
        }

        ShowCurrentQuestion();
    }

    public void SubmitChoice(int choiceIndex)
    {
        if (_quizCompleted || !HasCurrentProblem)
            return;

        if (_currentChoices == null || choiceIndex < 0 || choiceIndex >= _currentChoices.Length)
            return;

        MathProblemSO problem = CurrentProblem;
        if (problem.IsCorrect(_currentChoices[choiceIndex]))
        {
            _correctAnswerCount++;
            CorrectAnswerSubmitted?.Invoke(problem);
        }
        else
        {
            WrongAnswerSubmitted?.Invoke(problem);
        }

        MoveToNextQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (!HasCurrentProblem)
            return;

        _currentChoices = CurrentProblem.CreateShuffledChoices();
        if (_currentChoices == null)
        {
            quizUI.ShowConfigurationError();
            return;
        }

        quizUI.ShowQuestion(CurrentProblem.Question, CurrentQuestionNumber, QuestionCount, _currentChoices);
        QuestionShown?.Invoke(CurrentProblem);
    }

    private void CompleteQuiz()
    {
        _quizCompleted = true;
        quizUI.ShowQuizComplete(_correctAnswerCount, QuestionCount);
        QuizCompleted?.Invoke();
    }

    private void MoveToNextQuestion()
    {
        if (_currentQuestionIndex >= _questionOrder.Count - 1)
        {
            CompleteQuiz();
            return;
        }

        _currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    private static void Shuffle(List<MathProblemSO> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (items[i], items[randomIndex]) = (items[randomIndex], items[i]);
        }
    }
}
