using UnityEngine;
using UnityEngine.Events;

public class HappyEndingDialoguePanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private UnityEvent onDialogueShown;
    [SerializeField] private UnityEvent onDialogueFinished;

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        onDialogueShown?.Invoke();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // Connect this to the future dialogue UI's final-line or close callback.
    public void CompleteDialogue()
    {
        onDialogueFinished?.Invoke();
    }
}
