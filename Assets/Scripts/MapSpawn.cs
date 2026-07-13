using UnityEngine;
using UnityEngine.InputSystem;

public class MapSpawn : MonoBehaviour
{
    private OrderSO nowMap;
    [SerializeField] private Vector2 mapVector;
    private GameObject map;
    void Start()
    {
        SpawnMap();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Complete();
        }
    }

    void Complete()
    {
        Destroy(map);
        GameManager.Instance.RandomMap();
        SpawnMap();
    }

    void SpawnMap()
    {
        nowMap = GameManager.Instance.GiveData();
        if (!nowMap)
        {
            return;
        }
        map = Instantiate(nowMap.roomPrefab , mapVector, Quaternion.identity);
    }
}
