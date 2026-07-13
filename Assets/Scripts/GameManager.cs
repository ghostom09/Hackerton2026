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
    [SerializeField] private Behaviour[] playerGameplayBehaviours;

    public EndingType currentEnding { get; private set; } = EndingType.None;
    public int currentMapIndex { get; private set; } = -1;
    public int totalMapCount { get; private set; }
    public float currentMapTime { get; private set; }
    public float currentMapMaxTime { get; private set; }
    public int deathCount { get; private set; }
    public float totalPlayTime { get; private set; }
    public bool isGameEnded { get; private set; }

    private readonly List<OrderSO> _runtimeOrders = new();
    private GameObject _currentMap;
    private bool _isSwitchingMap;
    private Coroutine _nextMapRoutine;
    private Coroutine _badEndingPhoneRoutine;
    private BadEndingPhonePrompt _badEndingPhone;

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
        BuildRuntimeOrders();
        totalMapCount = _runtimeOrders.Count;
        GameResultManager.Instance?.BeginRun();
    }

    private void Start() => LoadNextMap();

    private void Update()
    {
        if (isGameEnded) return;
        totalPlayTime += UnityEngine.Time.deltaTime;
        UpdateMapTimer();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void StartMapTimer(float mapTime)
    {
        if (isGameEnded) return;
        currentMapMaxTime = Mathf.Max(0f, mapTime);
        currentMapTime = currentMapMaxTime;
        timer?.BeginOrder(currentMapMaxTime);
    }

    public void UpdateMapTimer()
    {
        if (isGameEnded || currentMapMaxTime <= 0f) return;
        currentMapTime = timer != null ? timer.RemainingTime : Mathf.Max(0f, currentMapTime - UnityEngine.Time.deltaTime);
        if (currentMapTime <= 0f) TriggerBadEnding();
    }

    public void CompleteCurrentMap()
    {
        if (isGameEnded) return;
        timer?.StopTimer();
        currentMapTime = timer != null ? timer.RemainingTime : currentMapTime;
    }

    public void RequestNextMap()
    {
        if (isGameEnded || _isSwitchingMap) return;
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

    public void NotifyFinalDoorOpened()
    {
        if (!isGameEnded && currentMapIndex >= totalMapCount - 1)
        {
            CompleteCurrentMap();
            TriggerHappyEnding();
        }
    }

    public void TriggerHappyEnding() => TriggerEnding(EndingType.Happy);

    public void TriggerBadEnding()
    {
        if (isGameEnded || _badEndingPhoneRoutine != null) return;
        deathCount++;
        isGameEnded = true;
        currentEnding = EndingType.Bad;
        currentMapTime = 0f;
        StopAllGameplay();
        UIManager.Instance?.ShowEmotion(charEmotion.mad);
        _badEndingPhoneRoutine = StartCoroutine(ShowBadEndingPhoneAfterDelay());
    }

    public void StopAllGameplay()
    {
        timer?.StopTimer();
        if (_nextMapRoutine != null) StopCoroutine(_nextMapRoutine);
        if (_currentMap != null) _currentMap.SetActive(false);

        // This optional inspector list is not assigned in InGameScene.
        // Iterating a null array caused the timeout ending to throw before changing scenes.
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

    private void LoadNextMap()
    {
        if (isGameEnded) return;
        if (_currentMap != null) Destroy(_currentMap);
        currentMapIndex++;
        if (currentMapIndex >= totalMapCount) { TriggerHappyEnding(); return; }

        NowMap = _runtimeOrders[currentMapIndex];
        if (NowMap == null || NowMap.roomPrefab == null) { TriggerBadEnding(); return; }
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
            TriggerHappyEnding();
        else
            LoadNextMap();
    }

    private void TriggerEnding(EndingType ending)
    {
        if (isGameEnded) return;
        isGameEnded = true;
        CompleteEnding(ending);
    }

    private IEnumerator ShowBadEndingPhoneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        _badEndingPhone ??= gameObject.AddComponent<BadEndingPhonePrompt>();
        _badEndingPhone.Show(CompleteBadEndingAfterPhoneAnswer);
    }

    private void CompleteBadEndingAfterPhoneAnswer()
    {
        if (currentEnding != EndingType.Bad) return;
        _badEndingPhoneRoutine = null;
        CompleteEnding(EndingType.Bad);
    }

    private void CompleteEnding(EndingType ending)
    {
        currentEnding = ending;
        currentMapTime = 0f;
        GameResultManager.Instance?.SaveResult(ending, totalPlayTime, deathCount, currentMapIndex, timer != null ? timer.RemainingTime : 0f);
        StopAllGameplay();
        var scene = ending == EndingType.Bad ? SceneName.SadEnding : SceneName.HappyEnding;
        if (SceneManager.Instance != null) SceneManager.Instance.ChangeScene(scene);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(ending == EndingType.Bad ? "BadEnding" : "HappyEnding");
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
