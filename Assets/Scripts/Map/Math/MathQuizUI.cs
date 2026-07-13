using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MathQuizUI : MonoBehaviour
{
    [Header("Blackboard UI")]
    [SerializeField] private Text questionText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Text[] choiceButtonLabels;

    public bool IsReady => questionText != null && progressText != null && feedbackText != null &&
                           choiceButtons != null && choiceButtons.Length == 4 &&
                           choiceButtonLabels != null && choiceButtonLabels.Length == 4;
    public int ChoiceCount => choiceButtons?.Length ?? 0;

    public Button GetChoiceButton(int index)
    {
        return index >= 0 && index < ChoiceCount ? choiceButtons[index] : null;
    }

    public void ShowQuestion(string question, int currentNumber, int totalNumber, string[] choices)
    {
        questionText.text = question;
        progressText.text = $"\uBB38\uC81C {currentNumber} / {totalNumber}";
        feedbackText.text = "\uB124 \uAC1C\uC758 \uC120\uD0DD\uC9C0 \uC911 \uC815\uB2F5\uC744 \uACE0\uB974\uC138\uC694.";
        feedbackText.color = new Color(0.86f, 0.9f, 0.82f);
        for (int i = 0; i < ChoiceCount; i++)
        {
            choiceButtons[i].gameObject.SetActive(true);
            choiceButtons[i].interactable = true;
            choiceButtonLabels[i].text = choices[i];
        }
    }

    public void ShowQuizComplete(int correctCount, int questionCount)
    {
        questionText.text = "\uC218\uC5C5 \uC885\uB8CC!";
        progressText.text = $"\uC815\uB2F5: {correctCount} / {questionCount}";
        feedbackText.text = "\uD034\uC988\uAC00 \uC885\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4!";
        feedbackText.color = new Color(0.55f, 1f, 0.62f);
        SetChoicesVisible(false);
    }

    public void ShowConfigurationError()
    {
        questionText.text = "\uC120\uD0DD\uC9C0 \uC124\uC815 \uD544\uC694";
        progressText.text = "MathProblemSO\uC5D0 \uC815\uB2F5 1\uAC1C\uC640 \uC624\uB2F5 3\uAC1C\uB97C \uC785\uB825\uD558\uC138\uC694.";
        feedbackText.text = string.Empty;
        SetChoicesVisible(false);
    }

    private void SetChoicesVisible(bool visible)
    {
        for (int i = 0; i < ChoiceCount; i++)
            choiceButtons[i].gameObject.SetActive(visible);
    }
}
