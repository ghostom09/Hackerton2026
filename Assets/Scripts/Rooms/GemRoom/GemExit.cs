using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GemExit : MonoBehaviour
{
    private GemRoomController room;

    private void Awake()
    {
        room = GetComponentInParent<GemRoomController>();

        if (room == null)
        {
            Debug.LogError(
                "부모에서 GemRoomController를 찾지 못했습니다.",
                this
            );
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (!other.CompareTag("Player"))
            return;

        if (room == null)
            return;

        room.TryClearRoom();
    }
}