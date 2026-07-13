using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HappyEndingDoor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite openedDoorSprite;
    [SerializeField] private Transform doorHandle;
    [SerializeField] private Transform zoomTarget;
    [SerializeField] private HappyEndingCameraController cameraController;

    [Header("Click Interaction")]
    [SerializeField, Min(1)] private int clicksToOpen = 8;
    [SerializeField] private float minimumHandleTurn = 4f;
    [SerializeField] private float maximumHandleTurn = 28f;
    [SerializeField] private float handleTurnDuration = 0.12f;
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float minimumShakeStrength = 0.025f;
    [SerializeField] private float maximumShakeStrength = 0.1f;

    public event Action Opened;

    public Transform ZoomTarget => zoomTarget != null ? zoomTarget : transform;

    private bool _canInteract;
    private bool _isOpen;
    private int _clickCount;
    private Quaternion _handleRestRotation;
    private Coroutine _handleRoutine;

    private void Awake()
    {
        if (doorRenderer == null)
            doorRenderer = GetComponent<SpriteRenderer>();

        if (cameraController == null && Camera.main != null)
            cameraController = Camera.main.GetComponent<HappyEndingCameraController>();

        if (doorHandle != null)
            _handleRestRotation = doorHandle.localRotation;
    }

    private void Update()
    {
        if (_canInteract && !_isOpen && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            RegisterClick();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        _canInteract = enabled && !_isOpen;
    }

    private void RegisterClick()
    {
        _clickCount++;
        var progress = Mathf.Clamp01((_clickCount - 1f) / Mathf.Max(1f, clicksToOpen - 1f));
        var turnAmount = Mathf.Lerp(minimumHandleTurn, maximumHandleTurn, progress);
        var shakeStrength = Mathf.Lerp(minimumShakeStrength, maximumShakeStrength, progress);

        cameraController?.Shake(shakeDuration, shakeStrength);

        if (_clickCount >= clicksToOpen)
        {
            _isOpen = true;
            _canInteract = false;
            if (_handleRoutine != null)
                StopCoroutine(_handleRoutine);
            _handleRoutine = StartCoroutine(OpenDoor(turnAmount));
            return;
        }

        if (_handleRoutine != null)
            StopCoroutine(_handleRoutine);
        _handleRoutine = StartCoroutine(TurnHandleAndReturn(turnAmount));
    }

    private IEnumerator TurnHandleAndReturn(float turnAmount)
    {
        yield return RotateHandle(_handleRestRotation, GetTurnedRotation(turnAmount), handleTurnDuration * 0.5f);
        yield return RotateHandle(GetTurnedRotation(turnAmount), _handleRestRotation, handleTurnDuration * 0.5f);
        _handleRoutine = null;
    }

    private IEnumerator OpenDoor(float turnAmount)
    {
        yield return RotateHandle(_handleRestRotation, GetTurnedRotation(turnAmount), handleTurnDuration);

        if (doorRenderer != null && openedDoorSprite != null)
            doorRenderer.sprite = openedDoorSprite;

        Opened?.Invoke();
        _handleRoutine = null;
    }

    private IEnumerator RotateHandle(Quaternion from, Quaternion to, float duration)
    {
        if (doorHandle == null)
            yield break;

        duration = Mathf.Max(duration, 0.01f);
        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            doorHandle.localRotation = Quaternion.Slerp(from, to, elapsed / duration);
            yield return null;
        }

        doorHandle.localRotation = to;
    }

    private Quaternion GetTurnedRotation(float turnAmount)
    {
        return _handleRestRotation * Quaternion.Euler(0f, 0f, -turnAmount);
    }
}
