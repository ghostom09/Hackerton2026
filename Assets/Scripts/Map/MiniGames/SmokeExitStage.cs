using UnityEngine;

[DisallowMultipleComponent]
public class SmokeExitStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform[] hazards;
    [SerializeField] private Vector2 roomClamp = new(5.8f, 3.4f);

    [Header("Hazard Spawn Area")]
    [Tooltip("가스 생성 범위의 중심 위치입니다.")]
    [SerializeField] private Vector2 hazardSpawnCenter = Vector2.zero;
    [Tooltip("중심에서 각 축으로 퍼지는 생성 범위입니다.")]
    [SerializeField] private Vector2 hazardSpawnRange = new(4.5f, 3.4f);

    [Header("Rules")]
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float hazardRadius = 0.7f;
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

        foreach (var hazard in hazards)
        {
            if (hazard == null)
                continue;

            var position = hazard.position;
            position.x = hazardSpawnCenter.x + Random.Range(-range.x, range.x);
            position.y = hazardSpawnCenter.y + Random.Range(-range.y, range.y);
            hazard.position = position;
        }
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
