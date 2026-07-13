using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GemCollectible : MonoBehaviour
{
    private GemRoomController room;
    private bool isCollected;

    public void Initialize(
        GemRoomController roomController
    )
    {
        room = roomController;
        isCollected = false;

        gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (isCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    private void Collect()
    {
        isCollected = true;

        room.CollectGem(this);

        gameObject.SetActive(false);
    }
}