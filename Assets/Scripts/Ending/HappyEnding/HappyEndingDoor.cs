using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HappyEndingDoor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private GameObject openedDoorPrefab;
    [SerializeField] private GameObject doorHandleObject;
    [SerializeField] private Transform zoomTarget;
    [Header("Click Interaction")]
    [SerializeField, Min(1)] private int clicksToOpen = 8;
    [SerializeField] private float minimumHandleTurn = 4f;
    [SerializeField] private float maximumHandleTurn = 28f;
    [SerializeField] private float handleTurnDuration = 0.2f;
    [SerializeField] private float openSpriteDelay = 0.4f;

    public event Action Opened;

    public Transform ZoomTarget => zoomTarget != null ? zoomTarget : transform;

    private bool _canInteract;
    private bool _isOpen;
    private int _clickCount;
    private float _handleRestAngle;
    private Coroutine _handleRoutine;
    private GameObject _openedDoorInstance;

    private void Awake()
    {
        if (doorRenderer == null)
            doorRenderer = GetComponent<SpriteRenderer>();

        if (doorHandleObject != null)
            _handleRestAngle = doorHandleObject.transform.localEulerAngles.z;
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

    public void HideHandle()
    {
        if (doorHandleObject != null)
            doorHandleObject.SetActive(false);
    }

    private void RegisterClick()
    {
        _clickCount++;
        var progress = Mathf.Clamp01((_clickCount - 1f) / Mathf.Max(1f, clicksToOpen - 1f));
        var turnAmount = Mathf.Lerp(minimumHandleTurn, maximumHandleTurn, progress);

        if (_clickCount >= clicksToOpen)
        {
            _isOpen = true;
            _canInteract = false;
            if (_handleRoutine != null)
                StopCoroutine(_handleRoutine);
            _handleRoutine = StartCoroutine(OpenDoorAfterHandleTurn(turnAmount));
            return;
        }

        if (_handleRoutine != null)
            StopCoroutine(_handleRoutine);
        _handleRoutine = StartCoroutine(TurnHandleAndReturn(turnAmount));
    }

    private IEnumerator TurnHandleAndReturn(float turnAmount)
    {
        var turnedAngle = _handleRestAngle - turnAmount;
        yield return RotateHandle(_handleRestAngle, turnedAngle, handleTurnDuration * 0.5f);
        yield return RotateHandle(turnedAngle, _handleRestAngle, handleTurnDuration * 0.5f);
        _handleRoutine = null;
    }

    private IEnumerator OpenDoorAfterHandleTurn(float turnAmount)
    {
        SetHandleAngle(_handleRestAngle - turnAmount);
        yield return new WaitForSecondsRealtime(openSpriteDelay);

        if (openedDoorPrefab != null && _openedDoorInstance == null)
        {
            _openedDoorInstance = Instantiate(openedDoorPrefab, transform.parent);
            _openedDoorInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
            _openedDoorInstance.transform.localScale = transform.localScale;
        }

        if (doorRenderer != null)
            doorRenderer.enabled = false;

        HideHandle();
        Opened?.Invoke();
        _handleRoutine = null;
    }

    private IEnumerator RotateHandle(float fromAngle, float toAngle, float duration)
    {
        if (doorHandleObject == null)
            yield break;

        duration = Mathf.Max(duration, 0.01f);
        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            SetHandleAngle(Mathf.LerpAngle(fromAngle, toAngle, elapsed / duration));
            yield return null;
        }

        SetHandleAngle(toAngle);
    }

    private void SetHandleAngle(float angle)
    {
        if (doorHandleObject != null)
            doorHandleObject.transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}
