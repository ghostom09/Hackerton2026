using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class HappyEndingDialoguePanel : MonoBehaviour
{
    public event Action DialogueCompleted;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject speakerNameRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private Color speakingCharacterColor = Color.white;
    [SerializeField] private Color nonSpeakingCharacterColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private UnityEvent onDialogueShown;
    [SerializeField] private UnityEvent onDialogueFinished;

    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string message;

        [Tooltip("Portrait setup applied together with this dialogue line.")]
        public PortraitState[] portraits;
    }

    [System.Serializable]
    public struct PortraitState
    {
        public string characterName;
        [Tooltip("Shown while this character is speaking. This image keeps its original color.")]
        public UnityEngine.UI.Image speakingImage;
        [Tooltip("Optional duplicate image shown while another character is speaking.")]
        public UnityEngine.UI.Image dimmedImage;
        [Tooltip("Optional expression sprite to apply on this dialogue line.")]
        public Sprite sprite;
    }

    private int _currentLineIndex;
    private bool _isShowing;

    private void Awake()
    {
        if (speakerNameRoot == null && speakerNameText != null)
            speakerNameRoot = speakerNameText.gameObject;
    }

    private void Update()
    {
        if (!_isShowing)
            return;

        var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        var keyboard = Keyboard.current;
        var nextKeyPressed = keyboard != null
            && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);

        if (mouseClicked || nextKeyPressed)
            ShowNextLine();
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        onDialogueShown?.Invoke();

        _currentLineIndex = 0;
        _isShowing = dialogueLines != null && dialogueLines.Length > 0;

        if (_isShowing)
            DisplayCurrentLine();
        else
            CompleteDialogue();
    }

    public void Hide()
    {
        _isShowing = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // This can also be connected to a UI button if the panel has one.
    public void ShowNextLine()
    {
        if (!_isShowing)
            return;

        _currentLineIndex++;
        if (_currentLineIndex >= dialogueLines.Length)
        {
            CompleteDialogue();
            return;
        }

        DisplayCurrentLine();
    }

    public void CompleteDialogue()
    {
        _isShowing = false;
        DialogueCompleted?.Invoke();
        onDialogueFinished?.Invoke();
    }

    private void DisplayCurrentLine()
    {
        var line = dialogueLines[_currentLineIndex];
        var hasSpeakerName = !string.IsNullOrWhiteSpace(line.speakerName);

        if (speakerNameRoot != null)
            speakerNameRoot.SetActive(hasSpeakerName);

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (dialogueText != null)
            dialogueText.text = line.message;

        UpdateCharacterPortraits(line.speakerName, line.portraits);
    }

    private void UpdateCharacterPortraits(string speakerName, PortraitState[] portraits)
    {
        if (portraits == null)
            return;

        foreach (var portrait in portraits)
        {
            if (portrait.speakingImage == null)
                continue;

            if (portrait.sprite != null)
            {
                if (portrait.speakingImage.sprite != portrait.sprite)
                    portrait.speakingImage.sprite = portrait.sprite;

                if (portrait.dimmedImage != null && portrait.dimmedImage.sprite != portrait.sprite)
                    portrait.dimmedImage.sprite = portrait.sprite;
            }

            var isSpeaking = !string.IsNullOrWhiteSpace(speakerName)
                && string.Equals(portrait.characterName, speakerName, StringComparison.Ordinal);

            // Use separate images when available: the speaking portrait is never tinted gray.
            if (portrait.dimmedImage != null && portrait.dimmedImage != portrait.speakingImage)
            {
                SetActive(portrait.speakingImage, isSpeaking);
                SetActive(portrait.dimmedImage, !isSpeaking);

                SetColor(portrait.speakingImage, speakingCharacterColor);
                SetColor(portrait.dimmedImage, nonSpeakingCharacterColor);
            }
            else
            {
                SetColor(portrait.speakingImage, isSpeaking ? speakingCharacterColor : nonSpeakingCharacterColor);
            }
        }
    }

    private static void SetActive(UnityEngine.UI.Image image, bool active)
    {
        if (image.gameObject.activeSelf != active)
            image.gameObject.SetActive(active);
    }

    private static void SetColor(UnityEngine.UI.Image image, Color color)
    {
        if (image.color != color)
            image.color = color;
    }
}
