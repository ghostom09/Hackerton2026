using System;
using UnityEngine;

/// <summary>PlayerMovement를 가진 플레이어가 닿으면 획득되는 쥬얼입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class JewelPickup : MonoBehaviour
{
    public event Action<JewelPickup> PickedUp;

    private bool isPickedUp;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp || other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        isPickedUp = true;
        PickedUp?.Invoke(this);
        Destroy(gameObject);
    }
}
