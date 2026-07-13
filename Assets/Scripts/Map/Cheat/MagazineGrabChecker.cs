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

    [Header("Mission Event")]
    [SerializeField] private UnityEvent onMagazineDiscarded;

    private Vector3 dragOffset;
    private bool isDragging;
    private bool isComplete;

    private void Awake()
    {
        FindCamera();

        if (createDefaultObjects && (magazine == null || trashBin == null))
        {
            CreateDefaultObjects();
        }

        if (magazine != null)
        {
            magazine.Discarded += NotifyMagazineDiscarded;
        }
    }

    private void OnDestroy()
    {
        if (magazine != null)
        {
            magazine.Discarded -= NotifyMagazineDiscarded;
        }
    }

    private void Update()
    {
        if (isComplete || Mouse.current == null || magazine == null)
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
            magazine.transform.position = ScreenToWorld(mousePosition) + dragOffset;

            if (trashBin != null && trashBin.TryDiscard(magazine))
            {
                return;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isDragging && trashBin != null)
            {
                trashBin.TryDiscard(magazine);
            }

            isDragging = false;
        }
    }

    private void TryStartDragging(Vector2 mousePosition)
    {
        if (!magazine.Contains(ScreenToWorld(mousePosition)))
        {
            return;
        }

        isDragging = true;
        dragOffset = magazine.transform.position - ScreenToWorld(mousePosition);
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        FindCamera();
        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        float depth = Mathf.Abs(magazine.transform.position.z - targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        worldPosition.z = magazine.transform.position.z;
        return worldPosition;
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
        isComplete = true;
        isDragging = false;
        onMagazineDiscarded?.Invoke();

        if (GameManager.Instance != null)
        {
            Debug.Log("clear");
            GameManager.Instance.NextMap();
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
