using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public List<OrderSO> mapData = new();
    public OrderSO nowMap;
    public int index;
    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        RandomMap();
    }

    public void ClearMap()
    {
        mapData.RemoveAt(index);
    }

    void RandomMap()
    {
        if (nowMap)
        {
            ClearMap();
        }
        
        int num = mapData.Count;
        
        index = UnityEngine.Random.Range(0, num);
        
        nowMap = mapData[index];
    }

    public OrderSO GiveData()
    {
        return nowMap;
    }
}
