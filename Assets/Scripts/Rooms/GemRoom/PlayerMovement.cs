using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            IsRunning = false;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        moveInput = new Vector2(
            horizontal,
            vertical
        ).normalized;

        bool shiftPressed =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        IsRunning = shiftPressed && IsMoving;
    }

    private void Move()
    {
        float speed = IsRunning
            ? runSpeed
            : walkSpeed;

        rb.linearVelocity = moveInput * speed;
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}