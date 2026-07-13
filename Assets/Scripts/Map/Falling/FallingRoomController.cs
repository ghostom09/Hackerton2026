using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FallingRoomController : MapBase
{
    [Header("Room")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Vector2 roomCenter;
    [SerializeField] private Vector2 roomSize = new Vector2(12f, 7f);
    [SerializeField] private float exitRadius = 0.8f;

    [Header("Falling Cover")]
    [SerializeField] private GameObject telegraphPrefab;
    [SerializeField] private GameObject fallingCoverPrefab;
    [SerializeField] private float telegraphDuration = 1f;
    [SerializeField] private float spawnInterval = 1.25f;
    [SerializeField] private float spawnIntervalVariance = 0.25f;
    [SerializeField] private float dropHeight = 8f;
    [SerializeField] private float coverFallSpeed = 12f;
    [SerializeField] private float coverRadius = 0.75f;
    [SerializeField] private float coverLifetimeAfterLanding = 1.25f;
    [SerializeField] private bool spawnNearPlayer = true;
    [SerializeField] private float spawnNearPlayerRadius = 2.75f;

    [Header("Rules")]
    [SerializeField] private bool failOnHit = true;
    [SerializeField] private bool startSpawningOnEnable = true;

    private Coroutine spawnRoutine;
    private bool isSpawning;

    public Transform Player => player;
    public bool FailOnHit => failOnHit;

    private void OnEnable()
    {
        if (startSpawningOnEnable)
        {
            BeginHazards();
        }
    }

    private void OnDisable()
    {
        StopHazards();
    }

    private void Update()
    {
        if (player == null || exitPoint == null)
        {
            return;
        }

        if (Vector2.Distance(player.position, exitPoint.position) <= exitRadius)
        {
            CompleteMission();
            StopHazards();
        }
    }

    private void OnValidate()
    {
        roomSize.x = Mathf.Max(0.1f, roomSize.x);
        roomSize.y = Mathf.Max(0.1f, roomSize.y);
        exitRadius = Mathf.Max(0.05f, exitRadius);
        telegraphDuration = Mathf.Max(0.05f, telegraphDuration);
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        spawnIntervalVariance = Mathf.Max(0f, spawnIntervalVariance);
        dropHeight = Mathf.Max(0.1f, dropHeight);
        coverFallSpeed = Mathf.Max(0.1f, coverFallSpeed);
        coverRadius = Mathf.Max(0.05f, coverRadius);
        coverLifetimeAfterLanding = Mathf.Max(0f, coverLifetimeAfterLanding);
        spawnNearPlayerRadius = Mathf.Max(0.1f, spawnNearPlayerRadius);
    }

    public void BeginHazards()
    {
        if (isSpawning)
        {
            return;
        }

        isSpawning = true;
        spawnRoutine = StartCoroutine(SpawnHazardsRoutine());
    }

    public void StopHazards()
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    public void NotifyPlayerHit()
    {
        if (!failOnHit)
        {
            return;
        }

        StopHazards();
        FailMission();
    }

    private IEnumerator SpawnHazardsRoutine()
    {
        while (isSpawning)
        {
            Vector3 landingPosition = PickLandingPosition();
            StartCoroutine(SpawnSingleHazard(landingPosition));

            float variance = Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            yield return new WaitForSeconds(Mathf.Max(0.05f, spawnInterval + variance));
        }
    }

    private IEnumerator SpawnSingleHazard(Vector3 landingPosition)
    {
        GameObject telegraph = SpawnTelegraph(landingPosition);
        yield return new WaitForSeconds(telegraphDuration);

        if (telegraph != null)
        {
            Destroy(telegraph);
        }

        SpawnFallingCover(landingPosition);
    }

    private Vector3 PickLandingPosition()
    {
        Vector2 halfSize = roomSize * 0.5f;
        Vector2 center = roomCenter;

        if (spawnNearPlayer && player != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnNearPlayerRadius;
            center = (Vector2)player.position + randomOffset;
        }

        float x = Mathf.Clamp(center.x, roomCenter.x - halfSize.x, roomCenter.x + halfSize.x);
        float y = Mathf.Clamp(center.y, roomCenter.y - halfSize.y, roomCenter.y + halfSize.y);

        if (!spawnNearPlayer || player == null)
        {
            x = Random.Range(roomCenter.x - halfSize.x, roomCenter.x + halfSize.x);
            y = Random.Range(roomCenter.y - halfSize.y, roomCenter.y + halfSize.y);
        }

        return new Vector3(x, y, transform.position.z);
    }

    private GameObject SpawnTelegraph(Vector3 landingPosition)
    {
        if (telegraphPrefab != null)
        {
            return Instantiate(telegraphPrefab, landingPosition, Quaternion.identity, transform);
        }

        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Quad);
        telegraph.name = "Falling Cover Telegraph";
        telegraph.transform.SetParent(transform);
        telegraph.transform.position = landingPosition;
        telegraph.transform.localScale = Vector3.one * (coverRadius * 2f);

        Collider collider = telegraph.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = telegraph.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.15f, 0.05f, 0.55f);
        }

        FallingTelegraph fallingTelegraph = telegraph.AddComponent<FallingTelegraph>();
        fallingTelegraph.Init(telegraphDuration);

        return telegraph;
    }

    private void SpawnFallingCover(Vector3 landingPosition)
    {
        Vector3 spawnPosition = landingPosition + Vector3.up * dropHeight;
        GameObject cover = fallingCoverPrefab != null
            ? Instantiate(fallingCoverPrefab, spawnPosition, Quaternion.identity, transform)
            : CreateDefaultCover(spawnPosition);

        FallingCover fallingCover = cover.GetComponent<FallingCover>();
        if (fallingCover == null)
        {
            fallingCover = cover.AddComponent<FallingCover>();
        }

        fallingCover.Init(this, landingPosition, coverFallSpeed, coverRadius, coverLifetimeAfterLanding);
    }

    private GameObject CreateDefaultCover(Vector3 spawnPosition)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = "Falling Cover";
        cover.transform.SetParent(transform);
        cover.transform.position = spawnPosition;
        cover.transform.localScale = Vector3.one * (coverRadius * 1.7f);

        Renderer renderer = cover.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.42f, 0.39f, 0.35f);
        }

        return cover;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(roomCenter.x, roomCenter.y, transform.position.z), new Vector3(roomSize.x, roomSize.y, 0f));

        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, exitRadius);
        }
    }
}
