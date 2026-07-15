using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Checks whether the player picked up the magazine, then follows the mouse
/// while it is held. The magazine and trash bin each own their own behaviour.
/// </summary>
[DisallowMultipleComponent]
public class MagazineGrabChecker : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private MagazineItem magazine;
    [SerializeField] private TrashBinDropZone trashBin;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool createDefaultObjects = true;
    [Min(1)] [SerializeField] private int magazineCount = 3;
    [SerializeField] private Vector2 additionalMagazineOffset = new Vector2(2.6f, 0f);

    [Header("Mission Event")]
    [SerializeField] private UnityEvent onMagazineDiscarded;

    private Vector3 dragOffset;
    private readonly List<MagazineItem> magazines = new();
    private MagazineItem draggedMagazine;
    private bool isDragging;
    private bool isComplete;
    private int discardedMagazineCount;

    private void Awake()
    {
        FindCamera();

        if (createDefaultObjects && (magazine == null || trashBin == null))
        {
            CreateDefaultObjects();
        }

        CreateMagazineCollection();

        foreach (MagazineItem magazineItem in magazines)
            magazineItem.Discarded += NotifyMagazineDiscarded;
    }

    private void OnDestroy()
    {
        foreach (MagazineItem magazineItem in magazines)
        {
            if (magazineItem != null)
                magazineItem.Discarded -= NotifyMagazineDiscarded;
        }
    }

    private void Update()
    {
        if (isComplete || Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDragging(mousePosition);
        }

        if (isDragging)
        {
            draggedMagazine.transform.position = ScreenToWorld(mousePosition) + dragOffset;

            if (trashBin != null && trashBin.TryDiscard(draggedMagazine))
            {
                return;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isDragging && trashBin != null)
            {
                trashBin.TryDiscard(draggedMagazine);
            }

            isDragging = false;
        }
    }

    private void TryStartDragging(Vector2 mousePosition)
    {
        Vector3 worldPosition = ScreenToWorld(mousePosition);
        foreach (MagazineItem magazineItem in magazines)
        {
            if (magazineItem == null || !magazineItem.Contains(worldPosition))
                continue;

            draggedMagazine = magazineItem;
            isDragging = true;
            dragOffset = draggedMagazine.transform.position - worldPosition;
            return;
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        FindCamera();
        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        MagazineItem referenceMagazine = GetReferenceMagazine();
        if (referenceMagazine == null)
            return Vector3.zero;

        float depth = Mathf.Abs(referenceMagazine.transform.position.z - targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        worldPosition.z = referenceMagazine.transform.position.z;
        return worldPosition;
    }

    private MagazineItem GetReferenceMagazine()
    {
        if (draggedMagazine != null)
            return draggedMagazine;

        foreach (MagazineItem magazineItem in magazines)
        {
            if (magazineItem != null)
                return magazineItem;
        }

        return magazine;
    }

    private void FindCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void NotifyMagazineDiscarded()
    {
        isDragging = false;
        draggedMagazine = null;
        discardedMagazineCount++;

        if (discardedMagazineCount < magazines.Count)
            return;

        isComplete = true;
        onMagazineDiscarded?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.NextMap();
    }

    private void CreateMagazineCollection()
    {
        if (magazine == null)
            return;

        magazines.Add(magazine);
        for (int i = 1; i < magazineCount; i++)
        {
            MagazineItem extraMagazine = Instantiate(magazine, magazine.transform.parent);
            extraMagazine.name = $"{magazine.name} {i + 1}";
            extraMagazine.transform.position = magazine.transform.position + (Vector3)(additionalMagazineOffset * i);
            magazines.Add(extraMagazine);
        }

        RandomizeMagazinePositions();
    }

    private void RandomizeMagazinePositions()
    {
        if (magazines.Count == 0)
            return;

        Physics2D.SyncTransforms();

        if (!TryGetSpawnBounds(out float minX, out float maxX, out float minY, out float maxY))
            return;

        GetMagazineSize(out float magazineWidth, out float magazineHeight);
        const float spacing = 0.02f;
        float horizontalSpacing = magazineWidth + spacing;
        float verticalSpacing = magazineHeight + spacing;

        // Keep the complete magazine inside the screen. The spawn area is the
        // left third of the screen, while its height uses the full screen.
        minX += magazineWidth * 0.5f;
        maxX -= magazineWidth * 0.5f;
        minY += magazineHeight * 0.5f;
        maxY -= magazineHeight * 0.5f;

        int columns = Mathf.FloorToInt((maxX - minX) / horizontalSpacing) + 1;
        int rows = Mathf.FloorToInt((maxY - minY) / verticalSpacing) + 1;

        if (columns <= 0 || rows <= 0 || columns * rows < magazines.Count)
        {
            Debug.LogWarning("Magazine spawn area is too small to place every magazine without overlap.", this);
            return;
        }

        // Place magazines on a randomly shifted, shuffled grid. The grid
        // spacing is based on collider size, so no two magazines can overlap.
        float horizontalOffset = Random.Range(0f, (maxX - minX) - horizontalSpacing * (columns - 1));
        float verticalOffset = Random.Range(0f, (maxY - minY) - verticalSpacing * (rows - 1));
        List<Vector2> slots = new(columns * rows);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                slots.Add(new Vector2(
                    minX + horizontalOffset + horizontalSpacing * column,
                    minY + verticalOffset + verticalSpacing * row));
            }
        }

        ShuffleSlots(slots);
        for (int i = 0; i < magazines.Count; i++)
        {
            MagazineItem magazineItem = magazines[i];
            if (magazineItem == null)
                continue;

            magazineItem.transform.position = new Vector3(
                slots[i].x,
                slots[i].y,
                magazineItem.transform.position.z);
        }
    }

    private bool TryGetSpawnBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        FindCamera();
        if (targetCamera == null)
        {
            minX = maxX = minY = maxY = 0f;
            Debug.LogWarning("Magazine spawn area could not find a camera.", this);
            return false;
        }

        MagazineItem referenceMagazine = GetReferenceMagazine();
        float depth = referenceMagazine == null
            ? 0f
            : Mathf.Abs(referenceMagazine.transform.position.z - targetCamera.transform.position.z);
        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
        Vector3 leftThird = targetCamera.ViewportToWorldPoint(new Vector3(1f / 3f, 0f, depth));

        minX = bottomLeft.x;
        maxX = leftThird.x;
        minY = bottomLeft.y;
        maxY = topLeft.y;
        return true;
    }

    private void GetMagazineSize(out float width, out float height)
    {
        width = 0f;
        height = 0f;
        foreach (MagazineItem magazineItem in magazines)
        {
            if (magazineItem != null && magazineItem.ItemCollider != null)
            {
                width = Mathf.Max(width, magazineItem.ItemCollider.bounds.size.x);
                height = Mathf.Max(height, magazineItem.ItemCollider.bounds.size.y);
            }
        }

        width = Mathf.Max(0.01f, width);
        height = Mathf.Max(0.01f, height);
    }

    private static void ShuffleSlots(List<Vector2> slots)
    {
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (slots[i], slots[swapIndex]) = (slots[swapIndex], slots[i]);
        }
    }

    private void CreateDefaultObjects()
    {
        if (magazine == null)
        {
            GameObject magazineObject = CreateRectangle("Magazine", new Vector2(-2.5f, 0f), new Vector2(1.8f, 2.4f), new Color(0.95f, 0.76f, 0.18f), 2);
            magazine = magazineObject.AddComponent<MagazineItem>();
            CreateRectangle("Magazine Title", new Vector2(0f, 0.55f), new Vector2(1.35f, 0.25f), Color.white, 0, magazine.transform);
            CreateRectangle("Magazine Photo", new Vector2(0f, -0.3f), new Vector2(1.25f, 0.95f), new Color(0.25f, 0.55f, 0.72f), 0, magazine.transform);
        }

        if (trashBin == null)
        {
            GameObject bin = CreateRectangle("Trash Bin", new Vector2(2.6f, -0.15f), new Vector2(1.75f, 2.5f), new Color(0.22f, 0.28f, 0.32f), 1);
            trashBin = bin.AddComponent<TrashBinDropZone>();
            CreateRectangle("Bin Lid", new Vector2(0f, 1.42f), new Vector2(2.05f, 0.28f), new Color(0.12f, 0.16f, 0.18f), 0, bin.transform);
            CreateRectangle("Bin Slot", new Vector2(0f, 0.75f), new Vector2(1.25f, 0.16f), Color.black, 0, bin.transform);
        }
    }

    private GameObject CreateRectangle(string objectName, Vector2 localPosition, Vector2 size, Color color, int sortingOrder, Transform parent = null)
    {
        GameObject rectangle = new GameObject(objectName);
        rectangle.transform.SetParent(parent == null ? transform : parent, false);
        rectangle.transform.localPosition = localPosition;

        SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        rectangle.transform.localScale = size;

        if (parent == null)
        {
            rectangle.AddComponent<BoxCollider2D>();
        }

        return rectangle;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }
}
