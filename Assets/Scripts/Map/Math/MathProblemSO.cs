using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MathProblem", menuName = "Mini Game/Math Problem")]
public class MathProblemSO : ScriptableObject
{
    [SerializeField, TextArea(2, 5)] private string question = "12 + 8 = ?";
    [SerializeField] private string answer = "20";
    [SerializeField] private string[] wrongAnswers = new string[3];

    public string Question => question;
    public string Answer => answer;

    public string[] CreateShuffledChoices()
    {
        if (wrongAnswers == null || wrongAnswers.Length != 3)
            return null;

        string[] choices = { answer, wrongAnswers[0], wrongAnswers[1], wrongAnswers[2] };
        for (int i = 0; i < choices.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(choices[i]))
                return null;

            for (int j = 0; j < i; j++)
            {
                if (string.Equals(Normalize(choices[i]), Normalize(choices[j]), StringComparison.OrdinalIgnoreCase))
                    return null;
            }
        }

        for (int i = choices.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (choices[i], choices[randomIndex]) = (choices[randomIndex], choices[i]);
        }

        return choices;
    }

    /// <summary>
    /// Compares answers while ignoring spaces and letter case.
    /// This lets a problem use either a number ("20") or a short expression ("x=20").
    /// </summary>
    public bool IsCorrect(string submittedAnswer)
    {
        return string.Equals(
            Normalize(submittedAnswer),
            Normalize(answer),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().Replace(" ", string.Empty);
    }

    private void OnValidate()
    {
        question ??= string.Empty;
        answer ??= string.Empty;

        if (wrongAnswers == null || wrongAnswers.Length != 3)
            Array.Resize(ref wrongAnswers, 3);
    }
}
