using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bug : MonoBehaviour
{
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [SerializeField] private BugCounter bugCounter;

    private Rigidbody2D _rb;
    private BugMovement _movement;
    private int _hits;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<BugMovement>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Sword"))
            return;

        TakeHit(other.transform.position);
    }

    private void TakeHit(Vector3 attackPosition)
    {
        _hits++;

        var knockbackDir = ((Vector2)(transform.position - attackPosition)).normalized;
        if (knockbackDir.sqrMagnitude < 0.0001f)
            knockbackDir = Vector2.up;

        if (_movement != null)
            _movement.ApplyKnockback(knockbackDir * knockbackForce, knockbackDuration);
        else
            _rb.linearVelocity = knockbackDir * knockbackForce;

        if (_hits < maxHits)
            return;

        if (bugCounter != null)
            bugCounter.AddKill();

        Destroy(gameObject);
    }
}
