using UnityEngine;
using UnityEngine.InputSystem;

public class TileMapPlayerMover : MonoBehaviour
{
    private TileMapMissionController controller;

    public Vector2Int Cell { get; private set; }

    public void Initialize(TileMapMissionController missionController, Vector2Int startCell)
    {
        controller = missionController;
        Cell = startCell;
    }

    public void SetCell(Vector2Int cell)
    {
        Cell = cell;
    }

    private void Update()
    {
        if (controller == null || Keyboard.current == null)
            return;

        Vector2Int direction = ReadDirection();
        if (direction != Vector2Int.zero)
            controller.TryMovePlayer(direction);
    }

    private Vector2Int ReadDirection()
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
}
