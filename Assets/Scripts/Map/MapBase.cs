using System;
using UnityEngine;

public abstract class MapBase : MonoBehaviour
{
    [SerializeField] private OrderSO order;
    
    public event Action OnMissionStarted;
    public event Action OnMissionCompleted;
    public event Action OnMissionFailed;

    private bool isRunning;
    private bool isFinished;

    public OrderSO Order => order;

    private void Start()
    {
        StartMission();
    }

    public void Init(OrderSO newOrder)
    {
        order = newOrder;
    }

    public void StartMission()
    {
        if (order == null)
        {
            Debug.LogError("OrderSO가 없습니다.", this);
            FailMission();
            return;
        }

        isRunning = true;
        isFinished = false;
        
        OnMissionStarted?.Invoke();
    }

    public void CompleteMission()
    {
        if (!isRunning || isFinished)
            return;

        isFinished = true;
        isRunning = false;

        OnMissionCompleted?.Invoke();
    }

    public void FailMission()
    {
        if (isFinished)
            return;

        isFinished = true;
        isRunning = false;

        OnMissionFailed?.Invoke();
    }
}
