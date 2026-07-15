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

        Vector3 firstMagazinePosition = magazine.transform.position;
        magazines.Add(magazine);
        for (int i = 1; i < magazineCount; i++)
        {
            MagazineItem extraMagazine = Instantiate(magazine, magazine.transform.parent);
            extraMagazine.name = $"{magazine.name} {i + 1}";
            extraMagazine.transform.position = magazine.transform.position + (Vector3)(additionalMagazineOffset * i);
            magazines.Add(extraMagazine);
        }

        RandomizeMagazinePositions(firstMagazinePosition);
    }

    private void RandomizeMagazinePositions(Vector3 firstMagazinePosition)
    {
        if (magazines.Count == 0)
            return;

        // Keep the random positions inside the span previously occupied by
        // the evenly spaced magazine copies.
        Vector3 lastMagazinePosition = firstMagazinePosition +
            (Vector3)(additionalMagazineOffset * (magazines.Count - 1));
        float minX = Mathf.Min(firstMagazinePosition.x, lastMagazinePosition.x);
        float maxX = Mathf.Max(firstMagazinePosition.x, lastMagazinePosition.x);
        float minY = Mathf.Min(firstMagazinePosition.y, lastMagazinePosition.y);
        float maxY = Mathf.Max(firstMagazinePosition.y, lastMagazinePosition.y);

        foreach (MagazineItem magazineItem in magazines)
        {
            if (magazineItem == null)
                continue;

            magazineItem.transform.position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                magazineItem.transform.position.z);
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
