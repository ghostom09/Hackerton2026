using UnityEngine;
using UnityEngine.InputSystem;

public class OutletHead : MonoBehaviour, IPointerDown, IPointerUp
{
    [SerializeField] private int frequency;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LineRenderer lineRenderer;

    private Collider2D _collider;
    private Vector3 _originPosition;
    private bool _isDragging;
    private float _z;

    public int Frequency => frequency;
    public bool IsConnected { get; private set; }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        _collider = GetComponent<Collider2D>();
        _originPosition = transform.position;
        _z = transform.position.z;

        if (lineRenderer != null)
            lineRenderer.positionCount = 2;

        UpdateLine();
    }

    private void Update()
    {
        if (!IsConnected && _isDragging && targetCamera != null)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var worldPos = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                worldPos.z = _z;
                transform.position = worldPos;
            }
        }

        UpdateLine();
    }

    public void OnPointerDown()
    {
        if (IsConnected)
            return;

        _isDragging = true;
    }

    public void OnPointerUp()
    {
        if (IsConnected)
            return;

        _isDragging = false;
        transform.position = _originPosition;
        UpdateLine();
    }

    public void ConnectTo(Vector3 position)
    {
        IsConnected = true;
        _isDragging = false;
        transform.position = position;

        if (_collider != null)
            _collider.enabled = false;

        UpdateLine();
    }

    private void UpdateLine()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, _originPosition);
        lineRenderer.SetPosition(1, transform.position);
    }
}
