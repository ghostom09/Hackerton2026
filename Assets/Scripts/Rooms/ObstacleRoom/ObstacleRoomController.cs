using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleRoomController : MonoBehaviour
{
    public event Action OnRoomFailed;
    public event Action OnRoomCleared;

    [Header("연결")]
    [SerializeField] private VerticalDodgePlayer player;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform scrollRoot;

    [Header("스크롤")]
    [SerializeField] private float scrollSpeed = 5f;

    [Header("Random obstacle placement")]
    [SerializeField, Min(0f)] private float obstacleClearance = 0.2f;
    [SerializeField, Min(0.1f)] private float minimumSafeBandHeight = 0.75f;
    [SerializeField, Min(1)] private int placementAttemptsPerObstacle = 32;

    private Vector3 originalScrollPosition;
    private ObstacleHazard[] obstacles;
    private readonly List<ScrollContentState> initialScrollContent = new();
    private bool isRunning;
    private bool isFinished;

    private struct VerticalInterval
    {
        public float Min;
        public float Max;

        public VerticalInterval(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    private struct ScrollContentState
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public bool WasActive;
    }

    private void Awake()
    {
        originalScrollPosition = scrollRoot.localPosition;
        obstacles = scrollRoot.GetComponentsInChildren<ObstacleHazard>(true);
        CacheInitialScrollContent();
    }

    private void Start()
    {
        player.OnObstacleHit += FailRoom;
        StartRoom();
    }

    private void Update()
    {
        if (isRunning)
        {
            ScrollStage();
        }

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartRoom();
        }
    }

    private void ScrollStage()
    {
        scrollRoot.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
    }

    public void StartRoom()
    {
        isRunning = true;
        isFinished = false;

        ResetScrollContent();

        Vector3 spawnPosition = playerSpawnPoint != null
            ? playerSpawnPoint.position
            : player.transform.position;

        player.ResetPlayer(spawnPosition);
        RandomizeObstacleLayout();
        Physics2D.SyncTransforms();

        Debug.Log("장애물 피하기 시작!");
    }

    private void CacheInitialScrollContent()
    {
        initialScrollContent.Clear();

        foreach (Transform content in scrollRoot.GetComponentsInChildren<Transform>(true))
        {
            if (content == scrollRoot)
                continue;

            initialScrollContent.Add(new ScrollContentState
            {
                Transform = content,
                LocalPosition = content.localPosition,
                LocalRotation = content.localRotation,
                LocalScale = content.localScale,
                WasActive = content.gameObject.activeSelf,
            });
        }
    }

    private void ResetScrollContent()
    {
        scrollRoot.gameObject.SetActive(true);
        scrollRoot.localPosition = originalScrollPosition;

        foreach (ScrollContentState content in initialScrollContent)
        {
            if (content.Transform == null)
                continue;

            content.Transform.localPosition = content.LocalPosition;
            content.Transform.localRotation = content.LocalRotation;
            content.Transform.localScale = content.LocalScale;
            content.Transform.gameObject.SetActive(content.WasActive);
        }
    }

    private void RandomizeObstacleLayout()
    {
        if (obstacles == null || obstacles.Length == 0)
            return;

        SetObstaclesActive(true);

        // A layout is accepted only when the player can move from one safe band
        // to a safe band at every obstacle before the next one arrives.
        for (int layoutAttempt = 0; layoutAttempt < placementAttemptsPerObstacle; layoutAttempt++)
        {
            if (TryGenerateLayout(true))
                return;
        }

        // Keep the latest random layout visible even when the reachability
        // validation cannot find a perfect route for the current settings.
        // Hiding every obstacle made a reset look like the stage had vanished.
        Debug.LogWarning("Unable to validate a traversable obstacle layout. Keeping the latest random obstacle positions.");
        SetObstaclesActive(true);
    }

    private void SetObstaclesActive(bool isActive)
    {
        foreach (ObstacleHazard obstacle in obstacles)
        {
            obstacle.gameObject.SetActive(isActive);
        }
    }

    private bool TryGenerateLayout(bool useRandomPositions)
    {
        List<ObstacleHazard> orderedObstacles = new List<ObstacleHazard>(obstacles);
        orderedObstacles.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null || scrollSpeed <= 0f)
            return false;

        List<VerticalInterval> reachableBands = new List<VerticalInterval>
        {
            new VerticalInterval(player.transform.position.y, player.transform.position.y)
        };

        Collider2D previousObstacle = null;

        foreach (ObstacleHazard obstacle in orderedObstacles)
        {
            Collider2D obstacleCollider = obstacle.GetComponent<Collider2D>();
            if (obstacleCollider == null)
                continue;

            if (!TryPlaceObstacle(
                    obstacle,
                    obstacleCollider,
                    playerCollider,
                    previousObstacle,
                    reachableBands,
                    useRandomPositions,
                    out List<VerticalInterval> nextReachableBands))
            {
                return false;
            }

            reachableBands = nextReachableBands;
            previousObstacle = obstacleCollider;
        }

        return reachableBands.Count > 0;
    }

    private bool TryPlaceObstacle(
        ObstacleHazard obstacle,
        Collider2D obstacleCollider,
        Collider2D playerCollider,
        Collider2D previousObstacle,
        List<VerticalInterval> reachableBands,
        bool useRandomPositions,
        out List<VerticalInterval> nextReachableBands)
    {
        Bounds obstacleBounds = obstacleCollider.bounds;
        float colliderCenterOffset = obstacleBounds.center.y - obstacle.transform.position.y;
        float inflatedHalfHeight = obstacleBounds.extents.y + playerCollider.bounds.extents.y + obstacleClearance;
        float minPositionY = player.MinY + inflatedHalfHeight - colliderCenterOffset;
        float maxPositionY = player.MaxY - inflatedHalfHeight - colliderCenterOffset;

        nextReachableBands = null;
        if (minPositionY > maxPositionY)
            return false;

        int attempts = useRandomPositions ? placementAttemptsPerObstacle : 64;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float positionY = useRandomPositions
                ? UnityEngine.Random.Range(minPositionY, maxPositionY)
                : Mathf.Lerp(minPositionY, maxPositionY, (attempt + 0.5f) / attempts);

            SetObstacleWorldY(obstacle.transform, positionY);

            List<VerticalInterval> safeBands = GetSafeBands(obstacleCollider, playerCollider.bounds.extents.y);
            float travelDistance = GetTravelDistance(playerCollider.bounds, obstacleCollider.bounds, previousObstacle);
            float maxVerticalTravel = player.VerticalSpeed * (travelDistance / scrollSpeed);
            List<VerticalInterval> expandedBands = ExpandBands(reachableBands, maxVerticalTravel);
            List<VerticalInterval> intersections = IntersectBands(expandedBands, safeBands);

            if (intersections.Count > 0)
            {
                nextReachableBands = intersections;
                return true;
            }
        }

        return false;
    }

    private void SetObstacleWorldY(Transform obstacleTransform, float positionY)
    {
        Vector3 position = obstacleTransform.position;
        position.y = positionY;
        obstacleTransform.position = position;
    }

    private List<VerticalInterval> GetSafeBands(Collider2D obstacleCollider, float playerHalfHeight)
    {
        Bounds bounds = obstacleCollider.bounds;
        float blockedMin = bounds.min.y - playerHalfHeight - obstacleClearance;
        float blockedMax = bounds.max.y + playerHalfHeight + obstacleClearance;
        List<VerticalInterval> safeBands = new List<VerticalInterval>();

        AddBandIfWideEnough(safeBands, player.MinY, Mathf.Min(blockedMin, player.MaxY));
        AddBandIfWideEnough(safeBands, Mathf.Max(blockedMax, player.MinY), player.MaxY);

        return safeBands;
    }

    private void AddBandIfWideEnough(List<VerticalInterval> bands, float min, float max)
    {
        if (max - min >= minimumSafeBandHeight)
            bands.Add(new VerticalInterval(min, max));
    }

    private float GetTravelDistance(Bounds playerBounds, Bounds currentObstacle, Collider2D previousObstacle)
    {
        if (previousObstacle == null)
        {
            return Mathf.Max(0f, currentObstacle.min.x - playerBounds.max.x);
        }

        return Mathf.Max(
            0f,
            currentObstacle.min.x - previousObstacle.bounds.max.x - playerBounds.size.x);
    }

    private List<VerticalInterval> ExpandBands(List<VerticalInterval> bands, float distance)
    {
        List<VerticalInterval> expandedBands = new List<VerticalInterval>(bands.Count);

        foreach (VerticalInterval band in bands)
        {
            expandedBands.Add(new VerticalInterval(
                Mathf.Max(player.MinY, band.Min - distance),
                Mathf.Min(player.MaxY, band.Max + distance)));
        }

        return expandedBands;
    }

    private List<VerticalInterval> IntersectBands(
        List<VerticalInterval> first,
        List<VerticalInterval> second)
    {
        List<VerticalInterval> intersections = new List<VerticalInterval>();

        foreach (VerticalInterval firstBand in first)
        {
            foreach (VerticalInterval secondBand in second)
            {
                float min = Mathf.Max(firstBand.Min, secondBand.Min);
                float max = Mathf.Min(firstBand.Max, secondBand.Max);
                AddBandIfWideEnough(intersections, min, max);
            }
        }

        return intersections;
    }

    private void FailRoom()
    {
        if (isFinished)
            return;

        Debug.Log("장애물 충돌! 스폰포인트로 돌아갑니다.");

        OnRoomFailed?.Invoke();

        // 플레이어와 스크롤을 처음 상태로 되돌린다.
        StartRoom();
    }

    public void ClearRoom()
    {
        if (isFinished)
            return;

        isRunning = false;
        isFinished = true;

        player.StopMovement();

        Debug.Log("장애물 방 클리어!");

        OnRoomCleared?.Invoke();

        // Keep this room on the same completion path as the other minigames.
        // GameManager handles the clear reaction and loads the next random stage.
        MiniGameClear.RequestNext();

        // 임시 확인용
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnObstacleHit -= FailRoom;
        }
    }
}
