using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FallingPlayerMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool clampToRoom;
    [SerializeField] private Vector2 roomCenter;
    [SerializeField] private Vector2 roomSize = new Vector2(12f, 7f);

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        Vector3 movement = new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
        transform.position += movement;

        if (clampToRoom)
        {
            ClampPositionToRoom();
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        roomSize.x = Mathf.Max(0.1f, roomSize.x);
        roomSize.y = Mathf.Max(0.1f, roomSize.y);
    }

    private Vector2 ReadMoveInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private void ClampPositionToRoom()
    {
        Vector2 halfSize = roomSize * 0.5f;
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, roomCenter.x - halfSize.x, roomCenter.x + halfSize.x);
        position.y = Mathf.Clamp(position.y, roomCenter.y - halfSize.y, roomCenter.y + halfSize.y);
        transform.position = position;
    }
}
