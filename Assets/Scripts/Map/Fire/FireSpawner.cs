using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FireSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private FireHealth smallFirePrefab;
    [SerializeField] private FireHealth largeFirePrefab;
    [SerializeField, Range(0f, 1f)] private float largeFireChance = 0.35f;
    [SerializeField] private bool setRandomFireSize = true;

    [Header("Spawn Count")]
    [SerializeField] private int spawnCount = 5;
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField] private bool clearBeforeSpawn = true;

    [Header("Spawn Area")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector2 areaCenter;
    [SerializeField] private Vector2 areaSize = new Vector2(13.8f, 6.81f);
    [SerializeField] private float spawnZ;

    private readonly List<FireHealth> spawnedFires = new();
    private int remainingLimit;

    public int RemainingLimit => remainingLimit;
    public int AliveFireCount => spawnedFires.Count;

    private void OnEnable()
    {
        if (spawnOnEnable)
            SpawnFires();
    }

    private void OnValidate()
    {
        spawnCount = Mathf.Max(0, spawnCount);
        areaSize.x = Mathf.Max(0.1f, areaSize.x);
        areaSize.y = Mathf.Max(0.1f, areaSize.y);
    }

    public void SpawnFires()
    {
        SpawnFires(spawnCount);
    }

    public void SpawnFires(int count)
    {
        if (clearBeforeSpawn)
            ClearSpawnedFires();

        count = Mathf.Max(0, count);
        remainingLimit = count;

        var availablePoints = CreateShuffledSpawnPoints();

        for (var i = 0; i < count; i++)
            SpawnSingleFire(PickSpawnPosition(availablePoints, i));

        if (remainingLimit <= 0)
            TryClearStage();
    }

    public void Register(FireHealth fire)
    {
        if (fire == null || spawnedFires.Contains(fire))
            return;

        spawnedFires.Add(fire);
    }

    public void NotifyFireCleared(FireHealth fire)
    {
        spawnedFires.Remove(fire);
        remainingLimit = Mathf.Max(0, remainingLimit - 1);

        if (remainingLimit <= 0)
            TryClearStage();
    }

    public void ClearSpawnedFires()
    {
        for (var i = spawnedFires.Count - 1; i >= 0; i--)
        {
            var fire = spawnedFires[i];
            if (fire == null)
            {
                spawnedFires.RemoveAt(i);
                continue;
            }

            Destroy(fire.gameObject);
        }

        spawnedFires.Clear();
        remainingLimit = 0;
    }

    private void TryClearStage()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RequestNextMap();
    }

    private void SpawnSingleFire(Vector3 position)
    {
        var fireSize = PickFireSize();
        var prefab = PickFirePrefab(fireSize);
        if (prefab == null)
        {
            Debug.LogWarning("FireSpawner has no fire prefab.", this);
            return;
        }

        var fire = Instantiate(prefab, position, Quaternion.identity, transform);
        if (setRandomFireSize)
            fire.SetFireSize(fireSize);

        fire.BindSpawner(this);
    }

    private FireSize PickFireSize()
    {
        return Random.value < largeFireChance ? FireSize.Large : FireSize.Small;
    }

    private FireHealth PickFirePrefab(FireSize fireSize)
    {
        if (fireSize == FireSize.Large && largeFirePrefab != null)
            return largeFirePrefab;

        if (fireSize == FireSize.Small && smallFirePrefab != null)
            return smallFirePrefab;

        return smallFirePrefab != null ? smallFirePrefab : largeFirePrefab;
    }

    private Vector3 PickRandomAreaPosition()
    {
        var halfSize = areaSize * 0.5f;
        var x = Random.Range(areaCenter.x - halfSize.x, areaCenter.x + halfSize.x);
        var y = Random.Range(areaCenter.y - halfSize.y, areaCenter.y + halfSize.y);
        return new Vector3(x, y, spawnZ);
    }

    private Vector3 PickSpawnPosition(List<Transform> availablePoints, int spawnIndex)
    {
        if (availablePoints.Count == 0)
            return PickRandomAreaPosition();

        if (spawnIndex < availablePoints.Count)
            return availablePoints[spawnIndex].position;

        return availablePoints[Random.Range(0, availablePoints.Count)].position;
    }

    private List<Transform> CreateShuffledSpawnPoints()
    {
        var points = new List<Transform>();

        if (spawnPoints == null)
            return points;

        for (var i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                points.Add(spawnPoints[i]);
        }

        for (var i = 0; i < points.Count; i++)
        {
            var randomIndex = Random.Range(i, points.Count);
            (points[i], points[randomIndex]) = (points[randomIndex], points[i]);
        }

        return points;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(areaCenter.x, areaCenter.y, spawnZ), new Vector3(areaSize.x, areaSize.y, 0f));

        if (spawnPoints == null)
            return;

        Gizmos.color = Color.red;
        for (var i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.2f);
        }
    }
}
