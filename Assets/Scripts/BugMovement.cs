using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BugMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minChangeInterval = 1f;
    [SerializeField] private float maxChangeInterval = 3f;
    [SerializeField] private Camera targetCamera;

    private Rigidbody2D _rb;
    private Vector2 _moveDirection;
    private float _nextDirectionTime;
    private bool _wasOutside;
    private float _knockbackEndTime;
    private Vector2 _knockbackVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        PickNewDirection();
    }

    private void FixedUpdate()
    {
        if (Time.time < _knockbackEndTime)
        {
            _rb.linearVelocity = _knockbackVelocity;
            return;
        }

        var isOutside = IsOutsideCamera();

        if (isOutside && !_wasOutside)
            ReverseDirection();
        else if (!isOutside && Time.time >= _nextDirectionTime)
            PickNewDirection();

        _wasOutside = isOutside;
        _rb.linearVelocity = _moveDirection * moveSpeed;
    }

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        _knockbackVelocity = velocity;
        _knockbackEndTime = Time.time + duration;
    }

    private bool IsOutsideCamera()
    {
        if (targetCamera == null)
            return false;

        var viewportPos = targetCamera.WorldToViewportPoint(transform.position);
        return viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f;
    }

    private void ReverseDirection()
    {
        _moveDirection = -_moveDirection;
        _nextDirectionTime = Time.time + Random.Range(minChangeInterval, maxChangeInterval);
    }

    private void PickNewDirection()
    {
        var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        _nextDirectionTime = Time.time + Random.Range(minChangeInterval, maxChangeInterval);
    }
}
