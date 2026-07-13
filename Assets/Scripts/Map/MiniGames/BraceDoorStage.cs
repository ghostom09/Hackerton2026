using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BraceDoorStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform door;
    [SerializeField] private Transform braceBar;

    [Header("Rules")]
    [SerializeField] private float requiredBrace = 0.85f;
    [SerializeField] private float pressureRate = 0.18f;
    [SerializeField] private float braceWhileHeld = 0.45f;
    [SerializeField] private float maxDoorOpen = 1.6f;
    [SerializeField] private float doorOpenSpeed = 0.3f;
    [SerializeField] private float doorCloseSpeed = 0.55f;
    [SerializeField] private float catchUpCloseSpeed = 1.1f;
    [SerializeField] private float braceBarMaxWidth = 6f;

    private float _brace;
    private float _pressure;
    private float _doorOpenAmount;
    private bool _complete;
    private Vector3 _doorClosedPos;
    private Vector3 _barBasePos;
    private Vector3 _barBaseScale;

    private void Awake()
    {
        if (door != null)
            _doorClosedPos = door.localPosition;
        if (braceBar != null)
        {
            _barBasePos = braceBar.localPosition;
            _barBaseScale = braceBar.localScale;
        }
    }

    private void Update()
    {
        if (_complete || door == null)
            return;

        _pressure += pressureRate * Time.deltaTime;

        var keyboard = Keyboard.current;
        var isBracing = (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.eKey.isPressed));

        // Bracing only works while the input remains held.
        _brace = isBracing
            ? Mathf.MoveTowards(_brace, 1f, braceWhileHeld * Time.deltaTime)
            : 0f;
        _pressure = Mathf.Clamp01(_pressure - _brace * 0.85f * Time.deltaTime);

        // A full brace always targets a closed door. While the brace is building,
        // a widely opened door catches up faster so it can still close in time.
        var braceStrength = Mathf.Clamp01(_brace / requiredBrace);
        var targetOpenAmount = _pressure * (1f - braceStrength);
        var closeGap = Mathf.Max(0f, _doorOpenAmount - targetOpenAmount);
        var moveSpeed = _doorOpenAmount < targetOpenAmount
            ? doorOpenSpeed
            : doorCloseSpeed + closeGap * catchUpCloseSpeed;
        _doorOpenAmount = Mathf.MoveTowards(_doorOpenAmount, targetOpenAmount, moveSpeed * Time.deltaTime);

        var open = Mathf.Lerp(0f, maxDoorOpen, _doorOpenAmount);
        door.localPosition = _doorClosedPos + new Vector3(open, 0f, 0f);

        if (braceBar != null)
        {
            var height = Mathf.Abs(_barBaseScale.y) > 0.001f ? _barBaseScale.y : 0.35f;
            braceBar.localScale = new Vector3(Mathf.Max(0.05f, _brace * braceBarMaxWidth), height, 1f);
            braceBar.localPosition = new Vector3(_barBasePos.x + braceBar.localScale.x * 0.5f, _barBasePos.y, _barBasePos.z);
        }

        if (_brace >= requiredBrace && _doorOpenAmount <= 0.02f)
            Complete();

    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
