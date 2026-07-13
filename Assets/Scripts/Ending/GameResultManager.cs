using System;
using UnityEngine;

[Serializable]
public class GameResultData
{
    public EndingType endingType;
    public float totalPlayTime;
    public int deathCount;
    public int lastMapIndex;
    public float lastMapRemainingTime;
    public bool isNewBest;
}

public sealed class GameResultManager : MonoBehaviour
{
    private const string LastResultKey = "Ending.LastResult";
    private const string BestHappyTimeKey = "Ending.BestHappyTime";
    public static GameResultManager Instance { get; private set; }
    public GameResultData CurrentResult { get; private set; } = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance == null) new GameObject(nameof(GameResultManager)).AddComponent<GameResultManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BeginRun() => CurrentResult = new GameResultData { endingType = EndingType.None };

    public void SaveResult(EndingType ending, float playTime, int deaths, int mapIndex, float remainingTime)
    {
        var isNewBest = false;
        if (ending == EndingType.Happy)
        {
            var best = PlayerPrefs.GetFloat(BestHappyTimeKey, -1f);
            isNewBest = best < 0f || playTime < best;
            if (isNewBest) PlayerPrefs.SetFloat(BestHappyTimeKey, playTime);
        }

        CurrentResult = new GameResultData
        {
            endingType = ending, totalPlayTime = playTime, deathCount = deaths,
            lastMapIndex = mapIndex, lastMapRemainingTime = remainingTime, isNewBest = isNewBest
        };
        PlayerPrefs.SetString(LastResultKey, JsonUtility.ToJson(CurrentResult));
        PlayerPrefs.Save();
    }

    public GameResultData LoadLastResult() => PlayerPrefs.HasKey(LastResultKey)
        ? JsonUtility.FromJson<GameResultData>(PlayerPrefs.GetString(LastResultKey))
        : new GameResultData { endingType = EndingType.None };
}
