using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class MathQuizInputHandler : MonoBehaviour
{
    private MathQuizController controller;
    private MathQuizUI quizUI;
    private UnityAction[] choiceActions;

    public void Bind(MathQuizController quizController, MathQuizUI view)
    {
        Unbind();
        controller = quizController;
        quizUI = view;
        choiceActions = new UnityAction[quizUI.ChoiceCount];

        for (int i = 0; i < choiceActions.Length; i++)
        {
            int choiceIndex = i;
            choiceActions[i] = () => SelectChoice(choiceIndex);
            quizUI.GetChoiceButton(i).onClick.AddListener(choiceActions[i]);
        }

    }

    private void OnDisable()
    {
        Unbind();
    }

    private void SelectChoice(int choiceIndex)
    {
        controller?.SubmitChoice(choiceIndex);
    }

    private void Unbind()
    {
        if (quizUI == null || !quizUI.IsReady)
            return;

        if (choiceActions != null)
        {
            for (int i = 0; i < choiceActions.Length; i++)
                quizUI.GetChoiceButton(i).onClick.RemoveListener(choiceActions[i]);
        }

        choiceActions = null;
        controller = null;
        quizUI = null;
    }
}
