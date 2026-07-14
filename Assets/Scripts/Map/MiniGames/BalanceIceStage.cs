using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BalanceIceStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform platform;
    [SerializeField] private Transform safeZone;
    [SerializeField] private Transform marker;

    [Header("Rules")]
    [SerializeField] private float holdSeconds = 3.5f;
    [SerializeField] private float driftSpeed = 1.8f;
    [SerializeField] private float driftLimit = 3.5f;
    [Tooltip("Ice visual cannot be found only when this fallback movement range is used.")]
    [SerializeField] private float markerHalfWidth = 2.2f;
    [Tooltip("Safe Zone visual cannot be found only when this fallback hit range is used.")]
    [SerializeField] private float safeRadius = 0.55f;
    [SerializeField] private float markerYOffset = 0.55f;

    private float _held;
    private float _driftDir = 1f;
    private bool _complete;
    private SpriteRenderer[] _platformRenderers;
    private SpriteRenderer[] _safeZoneRenderers;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);

        // Bounds are read from the renderers below, so changes to the Ice or
        // Safe Zone transform scale are reflected in the movement and hit area.
        if (platform != null)
            _platformRenderers = platform.GetComponentsInChildren<SpriteRenderer>(true);

        if (safeZone != null)
            _safeZoneRenderers = safeZone.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (_complete || platform == null || safeZone == null || marker == null)
            return;

        platform.position += Vector3.right * (_driftDir * driftSpeed * Time.deltaTime);
        if (Mathf.Abs(platform.position.x) > driftLimit)
            _driftDir *= -1f;

        if (Mouse.current != null)
        {
            var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
            var x = GetMarkerX(world.x);
            marker.position = new Vector3(x, platform.position.y + markerYOffset, 0f);
        }

        if (IsMarkerInSafeZone())
        {
            _held += Time.deltaTime;
            if (_held >= holdSeconds)
                Complete();
        }
        else
        {
            _held = Mathf.Max(0f, _held - Time.deltaTime * 1.5f);
        }
    }

    private float GetMarkerX(float targetX)
    {
        if (TryGetIceBounds(out var iceBounds))
            return Mathf.Clamp(targetX, iceBounds.min.x, iceBounds.max.x);

        return Mathf.Clamp(targetX, platform.position.x - markerHalfWidth, platform.position.x + markerHalfWidth);
    }

    private bool IsMarkerInSafeZone()
    {
        if (TryGetCombinedBounds(_safeZoneRenderers, out var safeBounds))
            return marker.position.x >= safeBounds.min.x && marker.position.x <= safeBounds.max.x;

        return Mathf.Abs(marker.position.x - safeZone.position.x) < safeRadius;
    }

    private bool TryGetIceBounds(out Bounds iceBounds)
    {
        if (_platformRenderers == null)
        {
            iceBounds = default;
            return false;
        }

        bool hasBounds = false;
        iceBounds = default;

        foreach (var renderer in _platformRenderers)
        {
            // Safe Zone is a child of Ice, but is not part of its playable width.
            if (renderer == null || !renderer.enabled || renderer.transform.IsChildOf(safeZone))
                continue;

            if (!hasBounds)
            {
                iceBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                iceBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool TryGetCombinedBounds(SpriteRenderer[] renderers, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (renderers == null)
            return false;

        bool hasBounds = false;
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
