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
    [SerializeField] private float gasMaxScale = 6f;
    [SerializeField] private float gasMinScale = 0.4f;
    [SerializeField] private float grabRadius = 1.6f;

    private bool _dragging;
    private bool _complete;
    private float _rotated;
    private float _lastAngle;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
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
            _rotated += Mathf.Abs(delta);
            valve.Rotate(0f, 0f, delta);
            _lastAngle = angle;

            if (gas != null)
            {
                var t = Mathf.Clamp01(1f - _rotated / requiredDegrees);
                gas.localScale = Vector3.one * Mathf.Lerp(gasMinScale, gasMaxScale, t);
            }

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
        if (gas != null)
            gas.localScale = Vector3.one * gasMinScale * 0.5f;
        MiniGameClear.RequestNext();
    }
}
