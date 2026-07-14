using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the Prologue dialogue and speaker focus.</summary>
public sealed class PrologueDialogueController : MonoBehaviour
{
    [Serializable]
    public struct DialogueLine
    {
        [Header("Speaking Character")]
        public string speakingCharacterName;
        public SpriteRenderer speakingCharacter;
        public Sprite speakingSprite;

        [Header("Non-Speaking Character")]
        public string nonSpeakingCharacterName;
        public SpriteRenderer nonSpeakingCharacter;
        public Sprite nonSpeakingSprite;

        [Header("Dialogue")]
        [TextArea(2, 4)] public string message;
    }

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] dialogueLines =
    {
        new DialogueLine { speakingCharacterName = "\uD50C\uB808\uC774\uC5B4", nonSpeakingCharacterName = "\uB3D9\uB8CC", message = "\uC624\uB298\uB3C4 \uAD6C\uC870 \uC694\uCCAD\uC774 \uB4E4\uC5B4\uC654\uC5B4." },
        new DialogueLine { speakingCharacterName = "\uB3D9\uB8CC", nonSpeakingCharacterName = "\uD50C\uB808\uC774\uC5B4", message = "\uC11C\uB450\uB974\uC790. \uB3C4\uC6C0\uC744 \uAE30\uB2E4\uB9AC\uB294 \uC0AC\uB78C\uB4E4\uC774 \uC788\uC5B4." },
        new DialogueLine { speakingCharacterName = "\uD50C\uB808\uC774\uC5B4", nonSpeakingCharacterName = "\uB3D9\uB8CC", message = "\uC54C\uACA0\uC5B4. \uBC14\uB85C \uCD9C\uBC1C\uD560\uAC8C." },
    };

    [Header("Scene Text UI (Optional)")]
    [Tooltip("Assign the Name Text (TMP) from the Prologue canvas.")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [Tooltip("Assign the Talk Text (TMP) from the Prologue canvas.")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "InGameScene";
    [SerializeField] private float blackFadeDuration = .6f;

    private readonly Dictionary<string, TextMeshProUGUI> speakerLabels = new();
    private int currentLine;
    private bool changingScene;

    private static readonly Color ActiveColor = Color.white;
    private static readonly Color InactiveColor = new(.42f, .42f, .46f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForPrologue()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Prologue"
            && FindFirstObjectByType<PrologueDialogueController>() == null)
            new GameObject(nameof(PrologueDialogueController)).AddComponent<PrologueDialogueController>();
    }

    private void Awake()
    {
        BuildDialogueUi();
        ShowCurrentLine();
    }

    private void Update()
    {
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool nextPressed = Keyboard.current != null
            && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (!changingScene && (clicked || nextPressed))
            Advance();
    }

    public void Advance()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        if (currentLine >= dialogueLines.Length - 1)
        {
            StartCoroutine(FadeOutAndLoadScene());
            return;
        }

        currentLine = Mathf.Min(currentLine + 1, dialogueLines.Length - 1);
        ShowCurrentLine();
    }

    private System.Collections.IEnumerator FadeOutAndLoadScene()
    {
        changingScene = true;

        var root = new GameObject("PrologueBlackFade", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var fade = CreatePanel("Black", root.transform, Color.black).GetComponent<Image>();
        Stretch(fade.rectTransform, Vector2.zero, Vector2.one);
        var color = fade.color;
        color.a = 0f;
        fade.color = color;

        float elapsed = 0f;
        while (elapsed < blackFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / blackFadeDuration);
            fade.color = color;
            yield return null;
        }

        SceneManager.Instance.ChangeScene(SceneName.InGameScene);
    }

    private void ShowCurrentLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0 || speakerNameText == null) return;

        var line = dialogueLines[Mathf.Clamp(currentLine, 0, dialogueLines.Length - 1)];
        speakerNameText.text = line.speakingCharacterName;
        dialogueText.text = line.message;

        ApplyCharacter(line.speakingCharacter, line.speakingSprite, true);
        ApplyCharacter(line.nonSpeakingCharacter, line.nonSpeakingSprite, false);

        foreach (var label in speakerLabels)
            label.Value.color = string.Equals(label.Key, line.speakingCharacterName, StringComparison.Ordinal) ? ActiveColor : InactiveColor;
    }

    private static void ApplyCharacter(SpriteRenderer character, Sprite sprite, bool speaking)
    {
        if (character == null) return;
        if (sprite != null) character.sprite = sprite;
        character.color = speaking ? ActiveColor : InactiveColor;
    }

    private void BuildDialogueUi()
    {
        // Use the designer-created texts when assigned in the Inspector.
        if (speakerNameText != null && dialogueText != null)
            return;

        var root = new GameObject("PrologueDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var dialoguePanel = CreatePanel("DialoguePanel", root.transform, new Color(.04f, .05f, .08f, .86f));
        Stretch(dialoguePanel.GetComponent<RectTransform>(), new Vector2(.08f, .05f), new Vector2(.92f, .29f));
        speakerNameText = CreateText("SpeakerName", dialoguePanel.transform, string.Empty, 34f, ActiveColor, TextAlignmentOptions.Left);
        Stretch(speakerNameText.rectTransform, new Vector2(.05f, .68f), new Vector2(.95f, .92f));
        dialogueText = CreateText("Dialogue", dialoguePanel.transform, string.Empty, 30f, ActiveColor, TextAlignmentOptions.Left);
        dialogueText.enableWordWrapping = true;
        Stretch(dialogueText.rectTransform, new Vector2(.05f, .14f), new Vector2(.95f, .66f));

        var names = new HashSet<string>();
        foreach (var line in dialogueLines)
        {
            if (!string.IsNullOrWhiteSpace(line.speakingCharacterName)) names.Add(line.speakingCharacterName);
            if (!string.IsNullOrWhiteSpace(line.nonSpeakingCharacterName)) names.Add(line.nonSpeakingCharacterName);
        }
        int index = 0;
        foreach (var name in names)
        {
            var label = CreateText("Speaker_" + index, root.transform, name, 30f, InactiveColor, TextAlignmentOptions.Center);
            float x = .25f + index * .5f;
            Stretch(label.rectTransform, new Vector2(x - .18f, .84f), new Vector2(x + .18f, .91f));
            speakerLabels[name] = label;
            index++;
        }

        var hint = CreateText("AdvanceHint", root.transform, "\uD074\uB9AD \uB610\uB294 Space", 22f, InactiveColor, TextAlignmentOptions.Center);
        Stretch(hint.rectTransform, new Vector2(.38f, .01f), new Vector2(.62f, .045f));
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
