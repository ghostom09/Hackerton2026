using UnityEngine;

/// <summary>Owns the trash-bin drop area and discards a magazine when it overlaps.</summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TrashBinDropZone : MonoBehaviour
{
    [SerializeField] private bool discardAsSoonAsItTouches = true;

    private Collider2D dropCollider;

    private void Awake()
    {
        dropCollider = GetComponent<Collider2D>();
    }

    public bool TryDiscard(MagazineItem magazine)
    {
        if (magazine == null || magazine.IsDiscarded || dropCollider == null || magazine.ItemCollider == null)
        {
            return false;
        }

        if (!discardAsSoonAsItTouches || !dropCollider.bounds.Intersects(magazine.ItemCollider.bounds))
        {
            return false;
        }

        magazine.Discard();
        return true;
    }
}
