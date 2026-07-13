using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class VerticalDodgePlayer : MonoBehaviour
{
    public event Action OnObstacleHit;

    [SerializeField] private float verticalSpeed = 6f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;

    private Rigidbody2D rb;
    private float verticalInput;
    private bool canMove = true;

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
        if (!canMove || Keyboard.current == null)
        {
            verticalInput = 0f;
            return;
        }

        verticalInput = 0f;

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            verticalInput += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            verticalInput -= 1f;
        }
    }

    private void Move()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = new Vector2(
            0f,
            verticalInput * verticalSpeed
        );

        Vector2 position = rb.position;

        position.y = Mathf.Clamp(
            position.y,
            minY,
            maxY
        );

        rb.position = position;
    }

    public void HitObstacle()
    {
        if (!canMove)
            return;

        canMove = false;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("장애물 충돌!");

        OnObstacleHit?.Invoke();
    }

    public void StopMovement()
    {
        canMove = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void ResetPlayer(Vector3 position)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.position = new Vector2(
            position.x,
            position.y
        );

        verticalInput = 0f;
        canMove = true;
    }
}