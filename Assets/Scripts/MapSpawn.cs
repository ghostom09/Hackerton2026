using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    // private (data) nonwMap;
    [SerializeField] private Vector2 mapVector;
    void Start()
    {
        SpawnMap();
    }

    void Complete()
    {
        // Destroy(nonwMap.);
        SpawnMap();
    }

    void SpawnMap()
    {
        // nowMap = GameManager.Instance.GiveData();
        // Instantiate(nowMap. , mapVector, Quaternion.identity);
    }
}
