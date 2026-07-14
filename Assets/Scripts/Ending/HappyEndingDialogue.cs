using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HappyEndingDialogue : MonoBehaviour
{
    [Header("캐릭터")]
    [SerializeField] private Image leftCharacter;
    [SerializeField] private Image rightCharacter;

    [Header("대화창")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    private readonly string[] dialogues =
    {
        "아아, 결국 넌 날 사랑하지 않았던거구나.",
        "아니라고? 넌 최선을 다 했다고?",
        "그게 뭐가 중요한거야. 최선을 다 하지 말고 모든 것을 바친다는 일념 하나로 왔어야지.",
        "넌 날 사랑하지 않는구나.",
        "더 이상 살아갈 수 없겠네, 너의 사랑은 나에겐 너무나도 중독적이고, 위험해서, 없다면 더 이상 살아갈 수 없으니까.",
        "설마 해방됐다고 생각하는거야? 음, 그래. 그럴리 없겠지.",
        "걱정마! 넌 나랑 영원히 여기서 지내는거야.",
        "그래. 몸은 떨어져 있어도, 마음은 가까이.",
        "그래, 결국 우린 같이 있게된거야. 비록 넌 날 사랑하지 않았겠지만. 난 널 사랑하니까",
        "…그래 나도 사랑해. 사랑하니 같이 이 갖가지 문제가 있는, 그런 곳에서 같이 잠들 수 있는 거겠지.",
        "아아, 이 어쩜 헌신적이고 낭만적인 사랑일까."
        
    };

    private readonly Color activeColor = Color.white;

    private readonly Color inactiveColor =
        new Color(0.35f, 0.35f, 0.35f, 1f);

    private int dialogueIndex;
    private bool dialogueFinished;

    private void Start()
    {
        dialoguePanel.SetActive(true);
        ShowDialogue();
    }

    private void Update()
    {
        if (dialogueFinished)
            return;

        bool pressSpace =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;

        bool clickMouse =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (pressSpace || clickMouse)
            NextDialogue();
    }

    private void ShowDialogue()
    {
        nameText.text = "유나";
        dialogueText.text = dialogues[dialogueIndex];

        // 오른쪽 유나가 말하고 있으므로 왼쪽을 어둡게 처리
        leftCharacter.color = inactiveColor;
        rightCharacter.color = activeColor;
    }

    private void NextDialogue()
    {
        dialogueIndex++;

        if (dialogueIndex >= dialogues.Length)
        {
            ShowDisconnectMessage();
            return;
        }

        ShowDialogue();
    }

    private void ShowDisconnectMessage()
    {
        dialogueFinished = true;

        nameText.text = string.Empty;
        dialogueText.text = "응답이 없습니다.";

        // 통신 종료 후 두 캐릭터 모두 어둡게 처리
        leftCharacter.color = inactiveColor;
        rightCharacter.color = inactiveColor;
    }
}