using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    private OrderSO nowMap;
    [SerializeField] private Vector2 mapVector;
    void Start()
    {
        SpawnMap();
    }

    void Complete()
    {
        Destroy(nowMap.roomPrefab);
        SpawnMap();
    }

    void SpawnMap()
    {
        nowMap = GameManager.Instance.GiveData();
        Instantiate(nowMap.roomPrefab , mapVector, Quaternion.identity);
    }
}
