using UnityEngine;

[DisallowMultipleComponent]
public class SmokeExitStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform[] hazards;
    [SerializeField] private Vector2 roomClamp = new Vector2(5.8f, 3.4f);

    [Header("Hazard Spawn Area")]
    [Tooltip("가스 생성 범위의 중심 위치입니다.")]
    [SerializeField] private Vector2 hazardSpawnCenter = Vector2.zero;
    [Tooltip("중심에서 각 축으로 퍼지는 생성 범위입니다.")]
    [SerializeField] private Vector2 hazardSpawnRange = new Vector2(2f, 4.2f);

    [Header("Rules")]
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float hazardRadius = 0.7f;
    [Tooltip("Fire에 닿아 시작 위치로 돌아갈 때 차감할 제한 시간(초)입니다.")]
    [SerializeField, Min(0f)] private float fireTimePenalty = 1f;
    [Tooltip("가스끼리 겹치지 않도록 유지할 중심 간 최소 거리입니다.")]
    [SerializeField] private float hazardSeparation = 1.4f;
    [SerializeField] private float exitRadius = 0.85f;

    private Vector3 _spawnPos;
    private bool _complete;

    private void Awake()
    {
        if (player != null)
            _spawnPos = player.position;

        if (hazards == null || hazards.Length == 0)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Fire"))
                    list.Add(child);
            }
            hazards = list.ToArray();
        }

        RandomizeHazards();
    }

    private void Update()
    {
        if (_complete || player == null || exitPoint == null)
            return;

        var input = MiniGameVisuals.ReadWasd();
        player.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
        var p = player.position;
        p.x = Mathf.Clamp(p.x, -roomClamp.x, roomClamp.x);
        p.y = Mathf.Clamp(p.y, -roomClamp.y, roomClamp.y);
        player.position = p;

        if (hazards != null)
        {
            for (var i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] == null)
                    continue;
                if (Vector2.Distance(hazards[i].position, player.position) < hazardRadius)
                {
                    player.position = _spawnPos;
                    GameManager.Instance?.ReduceCurrentMapTime(fireTimePenalty);
                    break;
                }
            }
        }

        if (Vector2.Distance(player.position, exitPoint.position) < exitRadius)
            Complete();
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }

    private void RandomizeHazards()
    {
        if (hazards == null)
            return;

        var range = new Vector2(
            Mathf.Max(0f, hazardSpawnRange.x),
            Mathf.Max(0f, hazardSpawnRange.y));

        var placedPositions = new System.Collections.Generic.List<Vector2>();
        var minimumDistance = Mathf.Max(0f, hazardSeparation);

        foreach (var hazard in hazards)
        {
            if (hazard == null)
                continue;

            var position = FindHazardSpawnPosition(range, placedPositions, minimumDistance);
            hazard.position = position;
            placedPositions.Add(position);
        }
    }

    private Vector2 FindHazardSpawnPosition(Vector2 range, System.Collections.Generic.List<Vector2> placedPositions, float minimumDistance)
    {
        const int maxAttempts = 40;
        var minimumDistanceSquared = minimumDistance * minimumDistance;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = new Vector2(
                hazardSpawnCenter.x + Random.Range(-range.x, range.x),
                hazardSpawnCenter.y + Random.Range(-range.y, range.y));

            var overlapsAnotherHazard = false;
            foreach (var placedPosition in placedPositions)
            {
                if ((candidate - placedPosition).sqrMagnitude < minimumDistanceSquared)
                {
                    overlapsAnotherHazard = true;
                    break;
                }
            }

            if (!overlapsAnotherHazard)
                return candidate;
        }

        // The configured spawn area is too crowded. Keep a valid candidate rather
        // than placing a gas directly on top of an existing one.
        return FindMostDistantSpawnPosition(range, placedPositions);
    }

    private Vector2 FindMostDistantSpawnPosition(Vector2 range, System.Collections.Generic.List<Vector2> placedPositions)
    {
        const int candidateCount = 100;
        var bestPosition = hazardSpawnCenter;
        var bestDistanceSquared = float.NegativeInfinity;

        for (var i = 0; i < candidateCount; i++)
        {
            var candidate = new Vector2(
                hazardSpawnCenter.x + Random.Range(-range.x, range.x),
                hazardSpawnCenter.y + Random.Range(-range.y, range.y));
            var nearestDistanceSquared = float.PositiveInfinity;

            foreach (var placedPosition in placedPositions)
                nearestDistanceSquared = Mathf.Min(nearestDistanceSquared, (candidate - placedPosition).sqrMagnitude);

            if (nearestDistanceSquared > bestDistanceSquared)
            {
                bestDistanceSquared = nearestDistanceSquared;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }

    private void OnDrawGizmosSelected()
    {
        var range = new Vector2(
            Mathf.Max(0f, hazardSpawnRange.x),
            Mathf.Max(0f, hazardSpawnRange.y));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hazardSpawnCenter, range * 2f);
    }
}
