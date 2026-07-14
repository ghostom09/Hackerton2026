using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera targetCamera;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        var desiredVelocity = _moveInput.normalized * moveSpeed;
        _rb.linearVelocity = ConstrainVelocityToCamera(desiredVelocity);
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
}
