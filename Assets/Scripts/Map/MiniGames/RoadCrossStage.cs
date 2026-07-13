using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoadCrossStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Vector2 roomClamp = new(5.5f, 3.6f);

    [Header("Rules")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float carSpeed = 5.5f;
    [SerializeField] private float spawnInterval = 0.85f;
    [SerializeField] private Vector2 laneYRange = new(-1.6f, 1.8f);
    [SerializeField] private float goalReachDistance = 1.2f;

    private readonly List<Transform> _cars = new();
    private Vector3 _spawnPos;
    private float _spawnTimer;
    private bool _complete;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;
        if (player != null)
            _spawnPos = player.position;
    }

    private void Update()
    {
        if (_complete || player == null)
            return;

        var input = MiniGameVisuals.ReadWasd();
        player.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
        var p = player.position;
        p.x = Mathf.Clamp(p.x, -roomClamp.x, roomClamp.x);
        p.y = Mathf.Clamp(p.y, -roomClamp.y, roomClamp.y);
        player.position = p;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnCar();
            _spawnTimer = spawnInterval;
        }

        for (var i = _cars.Count - 1; i >= 0; i--)
        {
            var car = _cars[i];
            if (car == null)
            {
                _cars.RemoveAt(i);
                continue;
            }

            var dir = Mathf.Sign(car.localScale.x);
            car.position += Vector3.right * (dir * carSpeed * Time.deltaTime);

            if (Mathf.Abs(car.position.x) > 8f)
            {
                Destroy(car.gameObject);
                _cars.RemoveAt(i);
                continue;
            }

            if (Vector2.Distance(car.position, player.position) < 0.85f)
                player.position = _spawnPos;
        }

        if (goal != null && Vector2.Distance(player.position, goal.position) <= goalReachDistance)
            Complete();
    }

    private void SpawnCar()
    {
        if (carPrefab == null)
            return;

        var laneY = Random.Range(laneYRange.x, laneYRange.y);
        var fromLeft = Random.value > 0.5f;
        var x = fromLeft ? -7f : 7f;
        var go = Instantiate(carPrefab, new Vector3(x, laneY, 0f), Quaternion.identity, spawnParent);
        var scale = go.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (fromLeft ? 1f : -1f);
        go.transform.localScale = scale;
        go.SetActive(true);
        _cars.Add(go.transform);
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
