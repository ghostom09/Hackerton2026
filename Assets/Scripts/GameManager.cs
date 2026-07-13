using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private OrderSO[] allOrders;
    [SerializeField] private Vector2 mapSpawnPosition;
    [SerializeField] private TextCore textCore;
    [SerializeField] private Timer timer;

    private readonly List<OrderSO> _runtimeOrders = new();
    private int _currentIndex = -1;
    private GameObject _currentMap;
    private bool _isSwitchingMap;
    private Coroutine _nextMapRoutine;

    public OrderSO NowMap { get; private set; }
    public int Time { get; private set; }

    // 기존 Timer 호환
    public int time => Time;
    public OrderSO nowMap => NowMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (textCore == null)
            textCore = FindFirstObjectByType<TextCore>();

        if (timer == null)
            timer = FindFirstObjectByType<Timer>();

        BuildRuntimeOrders();
    }

    private void Start()
    {
        NextMap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RequestNextMap()
    {
        if (_isSwitchingMap)
            return;

        _isSwitchingMap = true;

        if (_nextMapRoutine != null)
            StopCoroutine(_nextMapRoutine);

        _nextMapRoutine = StartCoroutine(NextMapEndOfFrame());
    }

    public void NextMap()
    {
        if (_currentMap != null)
        {
            Destroy(_currentMap);
            _currentMap = null;
        }

        _currentIndex++;

        if (_currentIndex >= _runtimeOrders.Count)
        {
            NowMap = null;
            Debug.Log("[GameManager] 더 이상 맵이 없습니다.");
            return;
        }

        NowMap = _runtimeOrders[_currentIndex];
        Time = Mathf.RoundToInt(NowMap.time);

        if (NowMap.roomPrefab == null)
        {
            Debug.LogError($"[GameManager] {NowMap.name}에 roomPrefab이 없습니다.", NowMap);
            return;
        }

        _currentMap = Instantiate(NowMap.roomPrefab, mapSpawnPosition, Quaternion.identity);

        if (_currentMap.TryGetComponent<MapBase>(out var mapBase))
            mapBase.Init(NowMap);

        ApplyOrderUI(NowMap);
    }

    private void ApplyOrderUI(OrderSO order)
    {
        if (timer != null)
            timer.BeginOrder(order.time);

        if (textCore != null && !string.IsNullOrEmpty(order.orderDialog))
            textCore.PlayText(order.orderDialog);
    }

    private IEnumerator NextMapEndOfFrame()
    {
        yield return null;
        _nextMapRoutine = null;
        _isSwitchingMap = false;
        NextMap();
    }

    public OrderSO GiveData()
    {
        return NowMap;
    }

    private void BuildRuntimeOrders()
    {
        _runtimeOrders.Clear();

        if (allOrders == null)
            return;

        foreach (var order in allOrders)
        {
            if (order != null)
                _runtimeOrders.Add(order);
        }

        Shuffle(_runtimeOrders);
        _currentIndex = -1;
    }

    private static void Shuffle(List<OrderSO> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

#if UNITY_EDITOR
    public void SetAllOrders(OrderSO[] orders)
    {
        allOrders = orders;
    }
#endif
}
