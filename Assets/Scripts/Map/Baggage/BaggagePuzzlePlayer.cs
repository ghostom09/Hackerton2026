using UnityEngine;
using UnityEngine.InputSystem;

public class BaggagePuzzlePlayer : MonoBehaviour
{
    private BaggagePuzzleController controller;

    public Vector2Int Cell { get; private set; }
    public Vector2Int FacingDirection { get; private set; } = Vector2Int.down;

    public void Init(BaggagePuzzleController puzzleController, Vector2Int startCell)
    {
        controller = puzzleController;
        Cell = startCell;
    }

    public void SetCell(Vector2Int cell)
    {
        Cell = cell;
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
            FacingDirection = direction;
    }

    private void Update()
    {
        if (controller == null || Keyboard.current == null)
            return;

        if (WasUndoPressed())
        {
            controller.TryReturnToStart();
            return;
        }

        Vector2Int direction = ReadInputDirection();
        if (direction == Vector2Int.zero)
            return;

        SetFacingDirection(direction);
        controller.TryMovePlayer(direction);
    }

    private Vector2Int ReadInputDirection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            return Vector2Int.up;

        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            return Vector2Int.down;

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            return Vector2Int.left;

        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            return Vector2Int.right;

        return Vector2Int.zero;
    }

    private bool WasUndoPressed()
    {
        Keyboard keyboard = Keyboard.current;
        bool isControlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        return isControlPressed && keyboard.zKey.wasPressedThisFrame;
    }
}
