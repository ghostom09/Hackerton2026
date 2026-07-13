using UnityEngine;

public class BaggagePuzzleCrate : MonoBehaviour
{
    public Vector2Int Cell { get; private set; }

    public void Init(Vector2Int startCell)
    {
        Cell = startCell;
    }

    public void SetCell(Vector2Int cell)
    {
        Cell = cell;
    }
}
