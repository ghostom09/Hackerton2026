using System;
using UnityEngine;

/// <summary>Represents the magazine object and is responsible only for its own disposal.</summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class MagazineItem : MonoBehaviour
{
    private Collider2D itemCollider;
    private bool isDiscarded;

    public event Action Discarded;
    public Collider2D ItemCollider => itemCollider;
    public bool IsDiscarded => isDiscarded;

    private void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
    }

    public bool Contains(Vector2 worldPosition)
    {
        return !isDiscarded && itemCollider != null && itemCollider.OverlapPoint(worldPosition);
    }

    public void Discard()
    {
        if (isDiscarded)
        {
            return;
        }

        isDiscarded = true;
        Discarded?.Invoke();
        Destroy(gameObject);
    }
}
