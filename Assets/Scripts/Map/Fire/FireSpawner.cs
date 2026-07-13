using System;
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
    [SerializeField] private Vector2 areaSize = new Vector2(8f, 5f);
    [SerializeField] private float spawnZ;

    private readonly List<FireHealth> spawnedFires = new();

    public event Action<FireHealth> FireSpawned;
    public event Action<FireHealth> FireExtinguished;
    public event Action AllFiresExtinguished;

    public int AliveFireCount => spawnedFires.Count;

    private void OnEnable()
    {
        if (spawnOnEnable)
        {
            SpawnFires();
        }
    }

    private void OnDisable()
    {
        UnsubscribeAll();
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
        {
            ClearSpawnedFires();
        }

        count = Mathf.Max(0, count);

        List<Transform> availablePoints = CreateShuffledSpawnPoints();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = PickSpawnPosition(availablePoints, i);

            SpawnSingleFire(spawnPosition);
        }

        if (spawnedFires.Count == 0)
        {
            AllFiresExtinguished?.Invoke();
        }
    }

    public void SpawnRandomFires()
    {
        SpawnFires();
    }

    public void ClearSpawnedFires()
    {
        for (int i = spawnedFires.Count - 1; i >= 0; i--)
        {
            FireHealth fire = spawnedFires[i];
            if (fire == null)
            {
                spawnedFires.RemoveAt(i);
                continue;
            }

            fire.Extinguished -= HandleFireExtinguished;
            Destroy(fire.gameObject);
        }

        spawnedFires.Clear();
    }

    private void SpawnSingleFire(Vector3 position)
    {
        FireSize fireSize = PickFireSize();
        FireHealth prefab = PickFirePrefab(fireSize);
        if (prefab == null)
        {
            Debug.LogWarning("FireSpawner has no fire prefab.", this);
            return;
        }

        FireHealth fire = Instantiate(prefab, position, Quaternion.identity, transform);
        if (setRandomFireSize)
        {
            fire.SetFireSize(fireSize);
        }

        fire.Extinguished += HandleFireExtinguished;

        spawnedFires.Add(fire);
        FireSpawned?.Invoke(fire);
    }

    private FireSize PickFireSize()
    {
        return UnityEngine.Random.value < largeFireChance ? FireSize.Large : FireSize.Small;
    }

    private FireHealth PickFirePrefab(FireSize fireSize)
    {
        if (fireSize == FireSize.Large && largeFirePrefab != null)
        {
            return largeFirePrefab;
        }

        if (fireSize == FireSize.Small && smallFirePrefab != null)
        {
            return smallFirePrefab;
        }

        return smallFirePrefab != null ? smallFirePrefab : largeFirePrefab;
    }

    private Vector3 PickRandomAreaPosition()
    {
        Vector2 halfSize = areaSize * 0.5f;
        float x = UnityEngine.Random.Range(areaCenter.x - halfSize.x, areaCenter.x + halfSize.x);
        float y = UnityEngine.Random.Range(areaCenter.y - halfSize.y, areaCenter.y + halfSize.y);

        return new Vector3(x, y, spawnZ);
    }

    private Vector3 PickSpawnPosition(List<Transform> availablePoints, int spawnIndex)
    {
        if (availablePoints.Count == 0)
        {
            return PickRandomAreaPosition();
        }

        if (spawnIndex < availablePoints.Count)
        {
            return availablePoints[spawnIndex].position;
        }

        return availablePoints[UnityEngine.Random.Range(0, availablePoints.Count)].position;
    }

    private List<Transform> CreateShuffledSpawnPoints()
    {
        List<Transform> points = new();

        if (spawnPoints == null)
        {
            return points;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                points.Add(spawnPoints[i]);
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, points.Count);
            Transform temp = points[i];
            points[i] = points[randomIndex];
            points[randomIndex] = temp;
        }

        return points;
    }

    private void HandleFireExtinguished(FireHealth fire)
    {
        fire.Extinguished -= HandleFireExtinguished;
        spawnedFires.Remove(fire);

        FireExtinguished?.Invoke(fire);

        if (spawnedFires.Count == 0)
        {
            AllFiresExtinguished?.Invoke();
        }
    }

    private void UnsubscribeAll()
    {
        for (int i = 0; i < spawnedFires.Count; i++)
        {
            if (spawnedFires[i] != null)
            {
                spawnedFires[i].Extinguished -= HandleFireExtinguished;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(areaCenter.x, areaCenter.y, spawnZ), new Vector3(areaSize.x, areaSize.y, 0f));

        if (spawnPoints == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.2f);
            }
        }
    }
}
