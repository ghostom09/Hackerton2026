using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BugMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minChangeInterval = 1f;
    [SerializeField] private float maxChangeInterval = 3f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private Camera targetCamera;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Vector2 _moveDirection;
    private float _nextDirectionTime;
    private bool _wasOutside;
    private float _knockbackEndTime;
    private Vector2 _knockbackVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        PickNewDirection();
    }

    private void FixedUpdate()
    {
        if (Time.time < _knockbackEndTime)
        {
            var knockbackVelocity = ConstrainVelocityToCamera(_knockbackVelocity);
            _rb.linearVelocity = knockbackVelocity;
            RotateTowardVelocity(knockbackVelocity);
            return;
        }

        var isOutside = IsOutsideCamera();

        if (isOutside && !_wasOutside)
            ReverseDirection();
        else if (!isOutside && Time.time >= _nextDirectionTime)
            PickNewDirection();

        _wasOutside = isOutside;
        var desiredVelocity = _moveDirection * moveSpeed;
        var constrainedVelocity = ConstrainVelocityToCamera(desiredVelocity);

        if ((constrainedVelocity - desiredVelocity).sqrMagnitude > 0.0001f)
            ReverseDirection();

        _rb.linearVelocity = constrainedVelocity;
        RotateTowardVelocity(constrainedVelocity);
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

    private Vector2 ConstrainVelocityToCamera(Vector2 desiredVelocity)
    {
        if (targetCamera == null)
            return desiredVelocity;

        var extents = _collider != null ? (Vector2)_collider.bounds.extents : Vector2.zero;
        var currentPosition = CameraBounds2D.ClampPosition(targetCamera, transform.position, extents);
        _rb.position = currentPosition;

        var nextPosition = CameraBounds2D.ClampPosition(
            targetCamera,
            (Vector3)currentPosition + (Vector3)(desiredVelocity * Time.fixedDeltaTime),
            extents);

        return (nextPosition - currentPosition) / Time.fixedDeltaTime;
    }

    private void PickNewDirection()
    {
        var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        _nextDirectionTime = Time.time + Random.Range(minChangeInterval, maxChangeInterval);
    }

    private void RotateTowardVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.0001f)
            return;

        var targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
        var nextAngle = Mathf.MoveTowardsAngle(_rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(nextAngle);
    }
}
