using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ObstacleFinish : MonoBehaviour
{
    private ObstacleRoomController room;

    private void Awake()
    {
        room =
            GetComponentInParent<ObstacleRoomController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        room.ClearRoom();
    }
}