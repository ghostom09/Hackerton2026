using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public List<OrderSO> mapData = new();
    public OrderSO nowMap;
    public int index;
    public int time;
    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        RandomMap();
    }

    void ClearMap()
    {
        mapData.RemoveAt(index);
    }

    public void RandomMap()
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
