using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float attackOffset = 0.6f;
    [SerializeField] private float attackRotationOffset = 0f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private Camera targetCamera;

    private float _nextAttackTime;
    private Vector2 _aimDirection = Vector2.right;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        UpdateAimDirection();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TryAttack();
    }

    private void UpdateAimDirection()
    {
        var mouse = Mouse.current;
        if (mouse == null || targetCamera == null)
            return;

        var worldPos = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        var dir = (Vector2)(worldPos - transform.position);

        if (dir.sqrMagnitude > 0.0001f)
            _aimDirection = dir.normalized;
    }

    private void TryAttack()
    {
        if (attackPrefab == null || Time.time < _nextAttackTime)
            return;

        _nextAttackTime = Time.time + cooldown;

        var angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg + attackRotationOffset;
        var spawnPos = (Vector2)transform.position + _aimDirection * attackOffset;
        var rotation = Quaternion.Euler(0f, 0f, angle);

        Instantiate(attackPrefab, spawnPos, rotation);
    }
}
