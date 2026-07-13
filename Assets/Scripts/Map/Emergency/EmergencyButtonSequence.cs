using System;
using UnityEngine;

/// <summary>정답 순서대로 배치된 UI Button 4개의 입력을 처리합니다.</summary>
public class EmergencyButtonSequence : MonoBehaviour
{
    [System.Serializable]
    private class AnswerButton
    {
        [Tooltip("이 버튼이 나타내는 응급환자 이미지")]
        public Sprite image;
        public UnityEngine.UI.Button button;
    }

    [SerializeField] private AnswerButton[] answerButtons = new AnswerButton[4];

    public event Action Completed;
    public event Action WrongAnswer;

    private int nextAnswerIndex;
    private bool acceptingInput;
    private int[] expectedButtonOrder;

    private void Awake()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null || answerButtons[i].button == null)
                continue;

            int buttonIndex = i;
            answerButtons[i].button.onClick.AddListener(() => OnButtonClicked(buttonIndex));
        }

        SetInteractable(false);
    }

    private void OnDisable()
    {
        acceptingInput = false;
        SetInteractable(false);
    }

    /// <summary>표시된 이미지 순서에 맞는 버튼 입력을 받기 시작합니다.</summary>
    public bool BeginInput(Sprite[] shownImages)
    {
        if (!HasValidSetup() || !TryBuildExpectedButtonOrder(shownImages))
            return false;

        nextAnswerIndex = 0;
        acceptingInput = true;
        SetInteractable(true);
        return true;
    }

    public void EndInput()
    {
        acceptingInput = false;
        SetInteractable(false);
    }

    private void OnButtonClicked(int clickedIndex)
    {
        if (!acceptingInput)
            return;

        if (expectedButtonOrder == null || nextAnswerIndex >= expectedButtonOrder.Length ||
            clickedIndex != expectedButtonOrder[nextAnswerIndex])
        {
            EndInput();
            WrongAnswer?.Invoke();
            return;
        }

        nextAnswerIndex++;
        if (nextAnswerIndex < expectedButtonOrder.Length)
            return;

        EndInput();
        Completed?.Invoke();
    }

    private void SetInteractable(bool interactable)
    {
        if (answerButtons == null)
            return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null && answerButtons[i].button != null)
                answerButtons[i].button.interactable = interactable;
        }
    }

    private bool HasValidSetup()
    {
        if (answerButtons == null || answerButtons.Length != 4)
        {
            Debug.LogError("EmergencyButtonSequence: 정답 버튼 4개를 순서대로 연결하세요.", this);
            return false;
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null || answerButtons[i].image == null || answerButtons[i].button == null)
            {
                Debug.LogError($"EmergencyButtonSequence: {i + 1}번 이미지 또는 버튼이 비어 있습니다.", this);
                return false;
            }
        }

        return true;
    }

    private bool TryBuildExpectedButtonOrder(Sprite[] shownImages)
    {
        if (shownImages == null || shownImages.Length != 4)
        {
            Debug.LogError("EmergencyButtonSequence: 표시 이미지 4개가 필요합니다.", this);
            return false;
        }

        expectedButtonOrder = new int[shownImages.Length];
        for (int imageIndex = 0; imageIndex < shownImages.Length; imageIndex++)
        {
            int matchingButtonIndex = -1;
            for (int buttonIndex = 0; buttonIndex < answerButtons.Length; buttonIndex++)
            {
                if (answerButtons[buttonIndex].image == shownImages[imageIndex])
                {
                    matchingButtonIndex = buttonIndex;
                    break;
                }
            }

            if (matchingButtonIndex < 0)
            {
                Debug.LogError($"EmergencyButtonSequence: 표시 순서 {imageIndex + 1}번 이미지와 연결된 버튼이 없습니다.", this);
                return false;
            }

            expectedButtonOrder[imageIndex] = matchingButtonIndex;
        }

        return true;
    }
}
