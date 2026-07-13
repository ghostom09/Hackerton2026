using UnityEngine;

public class TileMapExitDoor : MonoBehaviour
{
    private SpriteRenderer doorRenderer;
    private Color openColor;

    public bool IsOpen { get; private set; }

    public void Initialize(Sprite sprite, Vector3 position, float cellSize, Color lockedColor, Color newOpenColor)
    {
        transform.position = position;
        transform.localScale = new Vector3(cellSize * 0.58f, cellSize * 0.88f, 1f);

        doorRenderer = gameObject.AddComponent<SpriteRenderer>();
        doorRenderer.sprite = sprite;
        doorRenderer.color = lockedColor;
        doorRenderer.sortingOrder = 2;
        openColor = newOpenColor;
        IsOpen = false;
    }

    public void Open()
    {
        IsOpen = true;
        doorRenderer.color = openColor;
    }
}
