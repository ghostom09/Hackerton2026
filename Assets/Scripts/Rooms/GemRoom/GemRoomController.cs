using System;
using System.Collections.Generic;
using UnityEngine;

public class GemRoomController : MonoBehaviour
{
    public event Action<int, int> OnGemCountChanged;
    public event Action OnRoomCleared;

    [SerializeField] private GemCollectible[] gems;

    private readonly HashSet<GemCollectible> collectedGems = new();

    public int CollectedCount => collectedGems.Count;

    public int TotalGemCount =>
        gems == null ? 0 : gems.Length;

    public bool HasAllGems =>
        TotalGemCount > 0 &&
        CollectedCount >= TotalGemCount;

    public bool IsCleared { get; private set; }

    private void Start()
    {
        InitializeRoom();
    }

    private void InitializeRoom()
    {
        // Inspector에서 넣지 않았다면
        // 자식에 있는 보석을 자동으로 찾는다.
        if (gems == null || gems.Length == 0)
        {
            gems = GetComponentsInChildren<GemCollectible>(
                true
            );
        }

        collectedGems.Clear();
        IsCleared = false;

        foreach (GemCollectible gem in gems)
        {
            gem.Initialize(this);
        }

        Debug.Log(
            $"보석 방 시작: 총 {TotalGemCount}개"
        );
    }

    public void CollectGem(GemCollectible gem)
    {
        if (IsCleared || gem == null)
            return;

        // 같은 보석의 중복 획득 방지
        if (!collectedGems.Add(gem))
            return;

        OnGemCountChanged?.Invoke(
            CollectedCount,
            TotalGemCount
        );

        Debug.Log(
            $"보석 획득: " +
            $"{CollectedCount}/{TotalGemCount}"
        );

        if (HasAllGems)
        {
            Debug.Log(
                "모든 보석을 획득했습니다!"
            );
        }
    }

    public void TryClearRoom()
    {
        if (IsCleared)
            return;

        if (!HasAllGems)
        {
            Debug.Log(
                $"보석이 부족합니다: " +
                $"{CollectedCount}/{TotalGemCount}"
            );

            return;
        }

        IsCleared = true;

        Debug.Log("보석 방 클리어!");

        OnRoomCleared?.Invoke();

        // 임시 클리어 확인용
        gameObject.SetActive(false);
    }
}