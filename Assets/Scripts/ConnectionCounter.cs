using UnityEngine;

public class ConnectionCounter : MonoBehaviour
{
    [SerializeField] private int count;

    public int Count => count;

    public void OnConnected()
    {
        count--;
        if (count <= 0)
        {
            GameManager.Instance.RequestNextMap();
        }
    }
}
