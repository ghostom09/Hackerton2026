using UnityEngine;

public class FallingGoal : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float reachDistance = 0.8f;

    private bool _reached;

    private void Update()
    {
        if (_reached || player == null)
            return;

        if (Vector2.Distance(transform.position, player.position) > reachDistance)
            return;

        _reached = true;

        if (GameManager.Instance != null)
            GameManager.Instance.RequestNextMap();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reachDistance);
    }
}
