using UnityEngine;

public class BugCounter : MonoBehaviour
{
    [SerializeField] private int count;

    public int Count => count;

    public void AddKill()
    {
        count--;
        if (count <= 0)
        {
            GameManager.Instance.RequestNextMap();
        }
    }
}
