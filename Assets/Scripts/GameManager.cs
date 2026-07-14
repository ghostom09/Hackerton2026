using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private OrderSO[] allOrders;
    [SerializeField] private Vector2 mapSpawnPosition;
    [SerializeField] private TextCore textCore;
    [SerializeField] private Timer timer;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private Behaviour[] playerGameplayBehaviours;

    public int currentMapIndex { get; private set; } = -1;
    public int totalMapCount { get; private set; }
    public float currentMapTime { get; private set; }
    public float currentMapMaxTime { get; private set; }
    public float totalPlayTime { get; private set; }
    public bool isGameStopped { get; private set; }

    private readonly List<OrderSO> _runtimeOrders = new();
    private GameObject _currentMap;
    private bool _isSwitchingMap;
    private Coroutine _nextMapRoutine;
    private int _timerEmotionStage = -1;

    public OrderSO NowMap { get; private set; }
    public int Time => Mathf.CeilToInt(currentMapTime);
    public int time => Time;
    public OrderSO nowMap => NowMap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        textCore ??= FindFirstObjectByType<TextCore>();
        timer ??= FindFirstObjectByType<Timer>();
        countdownUI ??= FindFirstObjectByType<CountdownUI>();
        if (countdownUI == null)
            countdownUI = gameObject.AddComponent<CountdownUI>();

        if (timer != null)
            timer.TimeReduced += HandleTimerReduced;

        BuildRuntimeOrders();
        totalMapCount = _runtimeOrders.Count;
    }

    private void Start() => StartCoroutine(StartGameAfterCountdown());

    private void Update()
    {
        if (isGameStopped) return;
        totalPlayTime += UnityEngine.Time.deltaTime;
        UpdateMapTimer();
    }

    private void OnDestroy()
    {
        if (timer != null)
            timer.TimeReduced -= HandleTimerReduced;

        if (Instance == this) Instance = null;
    }

    public void StartMapTimer(float mapTime)
    {
        if (isGameStopped) return;
        currentMapMaxTime = Mathf.Max(0f, mapTime);
        currentMapTime = currentMapMaxTime;
        _timerEmotionStage = -1;
        timer?.BeginOrder(currentMapMaxTime);
        UpdateTimerEmotionByRemaining(true);
    }

    public void UpdateMapTimer()
    {
        if (isGameStopped || _isSwitchingMap || currentMapMaxTime <= 0f) return;
        currentMapTime = timer != null ? timer.RemainingTime : Mathf.Max(0f, currentMapTime - UnityEngine.Time.deltaTime);
        if (currentMapTime <= 0f)
        {
            StopRun();
            return;
        }

        UpdateTimerEmotionByRemaining(false);
    }

    public void CompleteCurrentMap()
    {
        if (isGameStopped) return;
        timer?.StopTimer();
        currentMapTime = timer != null ? timer.RemainingTime : currentMapTime;
    }

    public void RequestNextMap()
    {
        if (isGameStopped || _isSwitchingMap) return;
        CompleteCurrentMap();
        _isSwitchingMap = true;
        UIManager.Instance?.CompleteMap();
        _nextMapRoutine = StartCoroutine(NextMapAfterCompletionExpression());
    }

    public void MoveToNextMap() => RequestNextMap();

    public void NextMap()
    {
        if (NowMap == null) LoadNextMap();
        else RequestNextMap();
    }

    public void StopRun()
    {
        if (isGameStopped) return;
        isGameStopped = true;
        currentMapTime = 0f;
        UIManager.Instance?.ShowEmotion(charEmotion.menhara);
        StopAllGameplay();
    }

    public void StopAllGameplay()
    {
        timer?.StopTimer();
        if (_nextMapRoutine != null) StopCoroutine(_nextMapRoutine);
        if (_currentMap != null) _currentMap.SetActive(false);

        if (playerGameplayBehaviours != null)
        {
            foreach (var behaviour in playerGameplayBehaviours)
                if (behaviour != null) behaviour.enabled = false;
        }

        foreach (var movement in FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            movement.enabled = false;
        foreach (var attack in FindObjectsByType<PlayerAttack>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            attack.enabled = false;
        foreach (var input in FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            input.enabled = false;
    }

    public OrderSO GiveData() => NowMap;

    private IEnumerator StartGameAfterCountdown()
    {
        if (countdownUI != null)
            yield return countdownUI.Play();

        LoadNextMap();
    }

    private void LoadNextMap()
    {
        if (isGameStopped) return;
        if (_currentMap != null) Destroy(_currentMap);
        currentMapIndex++;
        if (currentMapIndex >= totalMapCount) { StopRun(); return; }

        NowMap = _runtimeOrders[currentMapIndex];
        if (NowMap == null || NowMap.roomPrefab == null) { StopRun(); return; }
        _currentMap = Instantiate(NowMap.roomPrefab, mapSpawnPosition, Quaternion.identity);
        if (_currentMap.TryGetComponent<MapBase>(out var mapBase)) mapBase.Init(NowMap);
        StartMapTimer(NowMap.time);
        if (textCore != null && !string.IsNullOrEmpty(NowMap.orderDialog)) textCore.PlayText(NowMap.orderDialog);
    }

    private IEnumerator NextMapAfterCompletionExpression()
    {
        float expressionDuration = UIManager.Instance != null
            ? UIManager.Instance.CompleteEmotionDuration
            : 0f;
        if (expressionDuration > 0f)
            yield return new WaitForSecondsRealtime(expressionDuration);

        _isSwitchingMap = false;
        _nextMapRoutine = null;
        if (currentMapIndex >= totalMapCount - 1)
            StopRun();
        else
            LoadNextMap();
    }

    private void HandleTimerReduced(float amount)
    {
        if (isGameStopped || amount <= 0f)
            return;

        UIManager.Instance?.ShowSadForTimerReduced();
    }

    private void UpdateTimerEmotionByRemaining(bool force)
    {
        if (UIManager.Instance == null || currentMapMaxTime <= 0f)
            return;

        var elapsedNormalized = Mathf.Clamp01(1f - (currentMapTime / currentMapMaxTime));
        var nextStage = elapsedNormalized < 1f / 3f ? 0 : elapsedNormalized < 2f / 3f ? 1 : 2;

        if (!force && nextStage == _timerEmotionStage)
            return;

        _timerEmotionStage = nextStage;

        var emotion = nextStage switch
        {
            0 => charEmotion.normal,
            1 => charEmotion.sad,
            _ => charEmotion.mad,
        };

        UIManager.Instance.ShowEmotion(emotion);
    }

    private void BuildRuntimeOrders()
    {
        _runtimeOrders.Clear();
        if (allOrders == null) return;
        foreach (var order in allOrders) if (order != null) _runtimeOrders.Add(order);
    }

#if UNITY_EDITOR
    public void SetAllOrders(OrderSO[] orders) => allOrders = orders;
#endif
}
