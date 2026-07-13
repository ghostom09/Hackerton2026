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
    [SerializeField] private float markerHalfWidth = 2.2f;
    [SerializeField] private float safeRadius = 0.55f;
    [SerializeField] private float markerYOffset = 0.55f;

    private float _held;
    private float _driftDir = 1f;
    private bool _complete;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
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
            var x = Mathf.Clamp(world.x, platform.position.x - markerHalfWidth, platform.position.x + markerHalfWidth);
            marker.position = new Vector3(x, platform.position.y + markerYOffset, 0f);
        }

        if (Mathf.Abs(marker.position.x - safeZone.position.x) < safeRadius)
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

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
