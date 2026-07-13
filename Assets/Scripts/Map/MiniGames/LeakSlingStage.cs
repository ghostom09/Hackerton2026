using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Slingshot arcade: pull and launch patches to seal spraying leaks before the flood rises.
/// </summary>
[DisallowMultipleComponent]
public class LeakSlingStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform slingAnchor;
    [SerializeField] private Transform pullMarker;
    [SerializeField] private Transform bandVisual;
    [SerializeField] private Transform floodFill;
    [SerializeField] private Transform overflowLine;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject patchPrefab;
    [SerializeField] private GameObject leakPrefab;

    [Header("Rules")]
    [SerializeField] private int requiredSeals = 6;
    [SerializeField] private float maxPull = 2.4f;
    [SerializeField] private float launchSpeed = 16f;
    [SerializeField] private float grabRadius = 1.4f;
    [SerializeField] private float hitRadius = 0.7f;
    [SerializeField] private float floodRisePerSecond = 0.07f;
    [SerializeField] private float floodOnMiss = 0.12f;
    [SerializeField] private float floodMaxHeight = 5.5f;
    [SerializeField] private float leakSpawnInterval = 0.9f;
    [SerializeField] private float leakLifetime = 3.2f;
    [SerializeField] private float arenaHalfWidth = 5.2f;
    [SerializeField] private float leakMinY = -0.4f;
    [SerializeField] private float leakMaxY = 3.4f;
    [SerializeField] private float projectileLife = 2.2f;

    [Header("Trajectory Preview")]
    [SerializeField] private int trajectoryPointCount = 12;
    [SerializeField, Range(0.1f, 1f)] private float trajectoryPreviewRatio = 0.22f;
    [SerializeField] private float trajectoryMaxDistance = 3.5f;
    [SerializeField] private float trajectoryDotSize = 0.1f;

    private readonly List<LeakBlob> _leaks = new();
    private readonly List<PatchShot> _shots = new();
    private readonly List<Transform> _trajectoryPoints = new();
    private bool _dragging;
    private bool _complete;
    private int _seals;
    private float _flood;
    private float _spawnTimer;
    private Vector3 _floodBasePos;
    private Vector3 _floodBaseScale;
    private Vector3 _bandBaseScale;
    private Sprite _trajectorySprite;

    private struct LeakBlob
    {
        public Transform Transform;
        public float Life;
    }

    private struct PatchShot
    {
        public Transform Transform;
        public Vector2 Velocity;
        public float Life;
    }

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (spawnParent == null)
            spawnParent = transform;

        if (floodFill != null)
        {
            _floodBasePos = floodFill.localPosition;
            _floodBaseScale = floodFill.localScale;

            if (overflowLine != null)
            {
                var overflowHeight = Mathf.Lerp(0.08f, floodMaxHeight, 0.98f);
                overflowLine.localPosition = new Vector3(
                    overflowLine.localPosition.x,
                    _floodBasePos.y + overflowHeight,
                    overflowLine.localPosition.z);
            }
        }

        if (bandVisual != null)
            _bandBaseScale = bandVisual.localScale;

        CreateTrajectoryPreview();

        if (pullMarker != null)
            pullMarker.gameObject.SetActive(false);
        if (bandVisual != null)
            bandVisual.gameObject.SetActive(false);

        _spawnTimer = 0.35f;
    }

    private void Update()
    {
        if (_complete || slingAnchor == null)
            return;

        HandleSling();
        UpdateLeaks();
        UpdateShots();
        UpdateFlood();
    }

    private void HandleSling()
    {
        if (Mouse.current == null)
            return;

        var mouse = Mouse.current.position.ReadValue();
        var world = MiniGameVisuals.ScreenToWorld(targetCamera, mouse, slingAnchor.position.z);

        if (!_dragging && Mouse.current.leftButton.wasPressedThisFrame &&
            Vector2.Distance(world, slingAnchor.position) <= grabRadius)
        {
            _dragging = true;
            if (pullMarker != null)
                pullMarker.gameObject.SetActive(true);
            if (bandVisual != null)
                bandVisual.gameObject.SetActive(true);
        }

        if (_dragging && Mouse.current.leftButton.isPressed)
        {
            var pull = Vector2.ClampMagnitude((Vector2)world - (Vector2)slingAnchor.position, maxPull);
            // Pull opposite of launch: drag down/back from anchor.
            if (pullMarker != null)
                pullMarker.position = slingAnchor.position + (Vector3)pull;

            UpdateBand(pull);
            UpdateTrajectoryPreview(pull);
        }

        if (_dragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            var pull = Vector2.ClampMagnitude(
                (pullMarker != null ? (Vector2)pullMarker.position : (Vector2)world) - (Vector2)slingAnchor.position,
                maxPull);
            Launch(-pull);
            _dragging = false;
            if (pullMarker != null)
                pullMarker.gameObject.SetActive(false);
            if (bandVisual != null)
                bandVisual.gameObject.SetActive(false);
            SetTrajectoryVisible(false);
        }
    }

    private void CreateTrajectoryPreview()
    {
        _trajectorySprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        for (var i = 0; i < trajectoryPointCount; i++)
        {
            var point = new GameObject("TrajectoryPoint");
            point.transform.SetParent(spawnParent, false);
            point.transform.localScale = Vector3.one * trajectoryDotSize;

            var renderer = point.AddComponent<SpriteRenderer>();
            renderer.sprite = _trajectorySprite;
            renderer.color = new Color(1f, 1f, 1f, 0.75f);
            renderer.sortingOrder = 4;

            point.SetActive(false);
            _trajectoryPoints.Add(point.transform);
        }
    }

    private void UpdateTrajectoryPreview(Vector2 pull)
    {
        var speed = launchSpeed * Mathf.Clamp01(pull.magnitude / maxPull);
        if (speed <= 0.01f)
        {
            SetTrajectoryVisible(false);
            return;
        }

        var velocity = -pull.normalized * speed;
        var previewTime = Mathf.Min(projectileLife * trajectoryPreviewRatio, trajectoryMaxDistance / speed);
        for (var i = 0; i < _trajectoryPoints.Count; i++)
        {
            var time = previewTime * (i + 1) / _trajectoryPoints.Count;
            var offset = velocity * time + Vector2.down * (4.5f * time * time);
            _trajectoryPoints[i].position = slingAnchor.position + (Vector3)offset;
            _trajectoryPoints[i].gameObject.SetActive(true);
        }
    }

    private void SetTrajectoryVisible(bool visible)
    {
        foreach (var point in _trajectoryPoints)
            point.gameObject.SetActive(visible);
    }

    private void UpdateBand(Vector2 pull)
    {
        if (bandVisual == null)
            return;

        var mid = (Vector2)slingAnchor.position + pull * 0.5f;
        var len = pull.magnitude;
        bandVisual.position = mid;
        bandVisual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg);
        var thickness = Mathf.Abs(_bandBaseScale.y) > 0.001f ? _bandBaseScale.y : 0.12f;
        bandVisual.localScale = new Vector3(Mathf.Max(0.05f, len), thickness, 1f);
    }

    private void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.08f)
            return;

        var vel = direction.normalized * (launchSpeed * Mathf.Clamp01(direction.magnitude / maxPull));
        var go = SpawnVisual(patchPrefab, slingAnchor.position, "Patch", new Color(0.95f, 0.75f, 0.25f), 0.45f);
        _shots.Add(new PatchShot
        {
            Transform = go.transform,
            Velocity = vel,
            Life = projectileLife
        });
    }

    private void UpdateLeaks()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f && _leaks.Count < 3)
        {
            SpawnLeak();
            _spawnTimer = leakSpawnInterval;
        }

        for (var i = _leaks.Count - 1; i >= 0; i--)
        {
            var leak = _leaks[i];
            if (leak.Transform == null)
            {
                _leaks.RemoveAt(i);
                continue;
            }

            leak.Life -= Time.deltaTime;
            // Bobble
            var p = leak.Transform.position;
            p.y += Mathf.Sin(Time.time * 4f + i) * 0.35f * Time.deltaTime;
            leak.Transform.position = p;
            _leaks[i] = leak;

            if (leak.Life > 0f)
                continue;

            Destroy(leak.Transform.gameObject);
            _leaks.RemoveAt(i);
            _flood = Mathf.Clamp01(_flood + floodOnMiss);
        }
    }

    private void UpdateShots()
    {
        for (var i = _shots.Count - 1; i >= 0; i--)
        {
            var shot = _shots[i];
            if (shot.Transform == null)
            {
                _shots.RemoveAt(i);
                continue;
            }

            shot.Transform.position += (Vector3)(shot.Velocity * Time.deltaTime);
            shot.Velocity += Vector2.down * (9f * Time.deltaTime);
            shot.Life -= Time.deltaTime;
            _shots[i] = shot;

            var hit = false;
            for (var j = _leaks.Count - 1; j >= 0; j--)
            {
                var leak = _leaks[j];
                if (leak.Transform == null)
                    continue;
                if (Vector2.Distance(shot.Transform.position, leak.Transform.position) > hitRadius)
                    continue;

                Destroy(leak.Transform.gameObject);
                _leaks.RemoveAt(j);
                _seals++;
                _flood = Mathf.Max(0f, _flood - 0.12f);
                hit = true;
                break;
            }

            if (hit || shot.Life <= 0f || Mathf.Abs(shot.Transform.position.x) > arenaHalfWidth + 2f ||
                shot.Transform.position.y < -4.5f)
            {
                Destroy(shot.Transform.gameObject);
                _shots.RemoveAt(i);
            }

            if (_seals >= requiredSeals)
            {
                Complete();
                return;
            }
        }
    }

    private void UpdateFlood()
    {
        _flood = Mathf.Clamp01(_flood + floodRisePerSecond * Time.deltaTime);
        if (floodFill != null)
        {
            var h = Mathf.Lerp(0.08f, floodMaxHeight, _flood);
            floodFill.localScale = new Vector3(_floodBaseScale.x, h, 1f);
            floodFill.localPosition = new Vector3(_floodBasePos.x, _floodBasePos.y + h * 0.5f, _floodBasePos.z);
        }

        if (_flood >= 0.98f)
        {
            _seals = Mathf.Max(0, _seals - 1);
            _flood = 0.45f;
        }
    }

    private void SpawnLeak()
    {
        var pos = new Vector3(
            Random.Range(-arenaHalfWidth, arenaHalfWidth),
            Random.Range(leakMinY, leakMaxY),
            0f);
        var go = SpawnVisual(leakPrefab, pos, "Leak", new Color(0.25f, 0.7f, 1f, 0.9f), Random.Range(0.55f, 0.8f));
        _leaks.Add(new LeakBlob { Transform = go.transform, Life = leakLifetime });
    }

    private GameObject SpawnVisual(GameObject prefab, Vector3 pos, string name, Color fallbackColor, float scale)
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
            sr.color = fallbackColor;
            sr.sortingOrder = 5;
        }

        go.name = name;
        go.SetActive(true);
        return go;
    }

    private void Complete()
    {
        _complete = true;
        _dragging = false;
        SetTrajectoryVisible(false);
        MiniGameClear.RequestNext();
    }
}
