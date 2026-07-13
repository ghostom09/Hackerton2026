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
    [SerializeField] private float bracePerClick = 0.14f;
    [SerializeField] private float braceWhileHeld = 0.45f;
    [SerializeField] private float maxDoorOpen = 1.6f;
    [SerializeField] private float braceBarMaxWidth = 6f;

    private float _brace;
    private float _pressure;
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

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                _brace += bracePerClick;
            if (Mouse.current.leftButton.isPressed)
                _brace += braceWhileHeld * Time.deltaTime;
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
            _brace += bracePerClick;

        _brace = Mathf.Clamp01(_brace);
        _pressure = Mathf.Clamp01(_pressure - _brace * 0.85f * Time.deltaTime);

        var open = Mathf.Lerp(0f, maxDoorOpen, Mathf.Clamp01(_pressure - _brace * 0.35f));
        door.localPosition = _doorClosedPos + new Vector3(open, 0f, 0f);

        if (braceBar != null)
        {
            var height = Mathf.Abs(_barBaseScale.y) > 0.001f ? _barBaseScale.y : 0.35f;
            braceBar.localScale = new Vector3(Mathf.Max(0.05f, _brace * braceBarMaxWidth), height, 1f);
            braceBar.localPosition = new Vector3(_barBasePos.x + braceBar.localScale.x * 0.5f, _barBasePos.y, _barBasePos.z);
        }

        if (_brace >= requiredBrace && _pressure < 0.55f)
            Complete();

        if (_pressure > 0.98f)
        {
            _brace = Mathf.Max(0f, _brace - 0.2f);
            _pressure = 0.6f;
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
