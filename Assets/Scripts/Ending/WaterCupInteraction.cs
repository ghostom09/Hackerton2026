using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shows an interaction prompt above a water cup while the player is nearby.
/// The ending controller enables this only after the wake-up scene is ready.
/// </summary>
public sealed class WaterCupInteraction : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactDistance = 1.35f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.65f, 0f);
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private UnityEvent onDrink;

    public bool IsEnabled { get; private set; }
    public bool WasUsed { get; private set; }
    public event Action Used;

    private GameObject _prompt;

    private void Awake()
    {
        if (player == null)
        {
            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null) player = taggedPlayer.transform;
        }

        CreatePrompt();
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!IsEnabled || WasUsed) return;

        var inRange = player != null && Vector3.Distance(player.position, transform.position) <= interactDistance;
        SetPromptVisible(inRange);
        if (inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Drink();
    }

    public void SetPlayer(Transform target) => player = target;

    public void EnableInteraction()
    {
        WasUsed = false;
        IsEnabled = true;
    }

    public void DisableInteraction()
    {
        IsEnabled = false;
        SetPromptVisible(false);
    }

    public IEnumerator WaitForDrink()
    {
        EnableInteraction();
        yield return new WaitUntil(() => WasUsed);
    }

    public void Drink()
    {
        if (!IsEnabled || WasUsed) return;
        WasUsed = true;
        DisableInteraction();
        onDrink?.Invoke();
        Used?.Invoke();
    }

    private void CreatePrompt()
    {
        if (promptPrefab != null)
        {
            _prompt = Instantiate(promptPrefab, transform);
            _prompt.transform.localPosition = promptOffset;
            return;
        }

        _prompt = new GameObject("WaterInteractionPrompt", typeof(Canvas), typeof(CanvasScaler));
        _prompt.transform.SetParent(transform, false);
        _prompt.transform.localPosition = promptOffset;
        var canvas = _prompt.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var rect = _prompt.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.4f, .52f);
        rect.localScale = Vector3.one * .01f;
        var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(_prompt.transform, false);
        text.text = "[E] 물 마시기";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineWidth = .2f;
        text.outlineColor = Color.black;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
    }

    private void SetPromptVisible(bool visible)
    {
        if (_prompt != null && _prompt.activeSelf != visible)
            _prompt.SetActive(visible);
    }
}
