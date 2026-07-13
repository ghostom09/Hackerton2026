using UnityEngine;
using UnityEngine.InputSystem;

public class PointerRaycaster : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask hitMask = ~0;

    private Collider2D _currentHover;
    private Collider2D _pressedHit;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (!ResolveCamera())
        {
            ClearPointerState();
            return;
        }

        var screenPos = mouse.position.ReadValue();
        if (!IsValidScreenPosition(screenPos))
            return;

        var worldPos = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        var hit = Physics2D.OverlapPoint(worldPos, hitMask);

        UpdateHover(hit);

        if (mouse.leftButton.wasPressedThisFrame && hit != null)
        {
            _pressedHit = hit;
            DispatchDown(hit);
            DispatchClick(hit);
        }

        if (mouse.leftButton.wasReleasedThisFrame && _pressedHit != null)
        {
            DispatchUp(_pressedHit);
            _pressedHit = null;
        }
    }

    private bool ResolveCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null && targetCamera.isActiveAndEnabled && targetCamera.pixelWidth > 0 && targetCamera.pixelHeight > 0;
    }

    private static bool IsValidScreenPosition(Vector2 screenPos)
    {
        return !(float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y) ||
                 float.IsInfinity(screenPos.x) || float.IsInfinity(screenPos.y));
    }

    private void ClearPointerState()
    {
        if (_currentHover != null)
        {
            DispatchHoverExit(_currentHover);
            _currentHover = null;
        }

        _pressedHit = null;
    }

    private void UpdateHover(Collider2D hit)
    {
        if (hit == _currentHover)
            return;

        if (_currentHover != null)
            DispatchHoverExit(_currentHover);

        _currentHover = hit;

        if (_currentHover != null)
            DispatchHover(_currentHover);
    }

    private static void DispatchHover(Collider2D hit)
    {
        foreach (var receiver in hit.GetComponents<IPointerHover>())
            receiver.OnPointerHover();
    }

    private static void DispatchHoverExit(Collider2D hit)
    {
        foreach (var receiver in hit.GetComponents<IPointerHoverExit>())
            receiver.OnPointerHoverExit();
    }

    private static void DispatchClick(Collider2D hit)
    {
        foreach (var receiver in hit.GetComponents<IPointerClick>())
            receiver.OnPointerClick();
    }

    private static void DispatchDown(Collider2D hit)
    {
        foreach (var receiver in hit.GetComponents<IPointerDown>())
            receiver.OnPointerDown();
    }

    private static void DispatchUp(Collider2D hit)
    {
        foreach (var receiver in hit.GetComponents<IPointerUp>())
            receiver.OnPointerUp();
    }
}
