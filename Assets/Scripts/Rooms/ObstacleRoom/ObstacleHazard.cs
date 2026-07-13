using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ObstacleHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        VerticalDodgePlayer player =
            other.GetComponent<VerticalDodgePlayer>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<VerticalDodgePlayer>();
        }

        if (player != null)
        {
            player.HitObstacle();
        }
    }
}