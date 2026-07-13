using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GasValveStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform valve;
    [SerializeField] private Transform gas;

    [Header("Rules")]
    [SerializeField] private float requiredDegrees = 540f;
    [SerializeField] private Vector3 gasFullScale = new Vector3(14f, 8f, 1f);
    [SerializeField] private float grabRadius = 1.6f;

    private bool _dragging;
    private bool _complete;
    private float _rotated;
    private float _lastAngle;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        UpdateGasVisual();
    }

    private void Update()
    {
        if (_complete || Mouse.current == null || valve == null)
            return;

        var mouse = Mouse.current.position.ReadValue();
        var world = MiniGameVisuals.ScreenToWorld(targetCamera, mouse, valve.position.z);
        var angle = Mathf.Atan2(world.y - valve.position.y, world.x - valve.position.x) * Mathf.Rad2Deg;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            Vector2.Distance(world, valve.position) <= grabRadius)
        {
            _dragging = true;
            _lastAngle = angle;
        }

        if (_dragging && Mouse.current.leftButton.isPressed)
        {
            var delta = Mathf.DeltaAngle(_lastAngle, angle);

            // Unity's positive Z rotation is counter-clockwise.  Only the
            // negative (clockwise) part of the mouse movement closes the valve.
            var clockwiseDelta = Mathf.Max(0f, -delta);
            _rotated += clockwiseDelta;
            valve.Rotate(0f, 0f, -clockwiseDelta);
            _lastAngle = angle;

            UpdateGasVisual();

            if (_rotated >= requiredDegrees)
                Complete();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            _dragging = false;
    }

    private void Complete()
    {
        _complete = true;
        _dragging = false;
        UpdateGasVisual();
        MiniGameClear.RequestNext();
    }

    private void UpdateGasVisual()
    {
        if (gas == null)
            return;

        var progress = Mathf.Clamp01(_rotated / requiredDegrees);
        gas.localScale = Vector3.Lerp(gasFullScale, Vector3.zero, progress);
    }
}
