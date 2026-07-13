using UnityEngine;

public class AttackProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.2f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
