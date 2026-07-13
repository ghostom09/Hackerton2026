using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Aim with mouse and stun incoming threats before they reach you.
/// </summary>
[DisallowMultipleComponent]
public class StunShootStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimPivot;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("Rules")]
    [SerializeField] private int requiredStuns = 8;
    [SerializeField] private float fireCooldown = 0.28f;
    [SerializeField] private float bulletSpeed = 16f;
    [SerializeField] private float enemySpeed = 4f;
    [SerializeField] private float spawnInterval = 0.6f;
    [SerializeField] private float hitRadius = 0.55f;
    [SerializeField] private float dangerRadius = 0.7f;
    [SerializeField] private float arenaRadius = 5.5f;

    private readonly List<Transform> _enemies = new();
    private readonly List<Transform> _bullets = new();
    private readonly List<Vector2> _bulletVel = new();
    private float _cooldown;
    private float _spawnTimer;
    private int _stuns;
    private bool _complete;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (spawnParent == null)
            spawnParent = transform;
        _spawnTimer = 0.35f;
    }

    private void Update()
    {
        if (_complete || player == null)
            return;

        UpdateAim();
        TryFire();
        SpawnEnemies();
        UpdateBullets();
        UpdateEnemies();
    }

    private void UpdateAim()
    {
        if (Mouse.current == null || aimPivot == null)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        var dir = ((Vector2)world - (Vector2)player.position);
        if (dir.sqrMagnitude < 0.001f)
            return;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        aimPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void TryFire()
    {
        _cooldown -= Time.deltaTime;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (_cooldown > 0f)
            return;

        _cooldown = fireCooldown;
        var angle = aimPivot != null ? aimPivot.localEulerAngles.z : 90f;
        var rad = angle * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        var spawnPos = (Vector2)player.position + dir * 0.55f;
        var go = Spawn(bulletPrefab, spawnPos, "Bullet", new Color(1f, 0.9f, 0.3f), 0.28f);
        _bullets.Add(go.transform);
        _bulletVel.Add(dir * bulletSpeed);
    }

    private void SpawnEnemies()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f || _enemies.Count >= 6)
            return;

        _spawnTimer = spawnInterval;
        var ang = Random.Range(0f, Mathf.PI * 2f);
        var pos = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * arenaRadius;
        var go = Spawn(enemyPrefab, pos, "Enemy", new Color(0.9f, 0.25f, 0.3f), 0.7f);
        _enemies.Add(go.transform);
    }

    private void UpdateBullets()
    {
        for (var i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            if (b == null)
            {
                _bullets.RemoveAt(i);
                _bulletVel.RemoveAt(i);
                continue;
            }

            b.position += (Vector3)(_bulletVel[i] * Time.deltaTime);
            if (b.position.magnitude > arenaRadius + 1.5f)
            {
                Destroy(b.gameObject);
                _bullets.RemoveAt(i);
                _bulletVel.RemoveAt(i);
                continue;
            }

            var hit = false;
            for (var j = _enemies.Count - 1; j >= 0; j--)
            {
                var e = _enemies[j];
                if (e == null)
                    continue;
                if (Vector2.Distance(b.position, e.position) > hitRadius)
                    continue;

                Destroy(e.gameObject);
                _enemies.RemoveAt(j);
                _stuns++;
                hit = true;
                if (_stuns >= requiredStuns)
                {
                    Destroy(b.gameObject);
                    _bullets.RemoveAt(i);
                    _bulletVel.RemoveAt(i);
                    Complete();
                    return;
                }
                break;
            }

            if (!hit)
                continue;

            Destroy(b.gameObject);
            _bullets.RemoveAt(i);
            _bulletVel.RemoveAt(i);
        }
    }

    private void UpdateEnemies()
    {
        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            var e = _enemies[i];
            if (e == null)
            {
                _enemies.RemoveAt(i);
                continue;
            }

            var dir = ((Vector2)player.position - (Vector2)e.position);
            if (dir.sqrMagnitude > 0.001f)
                e.position += (Vector3)(dir.normalized * (enemySpeed * Time.deltaTime));

            if (Vector2.Distance(e.position, player.position) < dangerRadius)
            {
                Destroy(e.gameObject);
                _enemies.RemoveAt(i);
                _stuns = Mathf.Max(0, _stuns - 1);
            }
        }
    }

    private GameObject Spawn(GameObject prefab, Vector3 pos, string name, Color color, float scale)
    {
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
            go.transform.localScale = Vector3.one * scale;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(spawnParent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = color;
            sr.sortingOrder = 5;
        }

        go.name = name;
        go.SetActive(true);
        return go;
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
