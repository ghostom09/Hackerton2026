using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Whack-a-mole: smash popping hazards before they retreat.
/// </summary>
[DisallowMultipleComponent]
public class HazardWhackStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform[] holes;
    [SerializeField] private Transform progressFill;
    [SerializeField] private GameObject hazardPrefab;

    [Header("Rules")]
    [SerializeField] private int requiredHits = 10;
    [SerializeField] private float popInterval = 0.55f;
    [SerializeField] private float upSeconds = 0.85f;
    [SerializeField] private float maxActive = 3;
    [SerializeField] private float hitRadius = 0.75f;
    [SerializeField] private float progressMaxWidth = 4f;

    private struct Slot
    {
        public Transform Hazard;
        public float Life;
        public bool Active;
    }

    private Slot[] _slots;
    private float _spawnTimer;
    private int _hits;
    private bool _complete;
    private Vector3 _progressBasePos;
    private Vector3 _progressBaseScale;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        EnsureHoles();
        _slots = new Slot[holes.Length];
        _spawnTimer = 0.25f;

        if (progressFill != null)
        {
            _progressBasePos = progressFill.localPosition;
            _progressBaseScale = progressFill.localScale;
        }

        UpdateProgress();
    }

    private void Update()
    {
        if (_complete)
            return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            TryPop();
            _spawnTimer = popInterval * Random.Range(0.7f, 1.15f);
        }

        UpdateSlots();
        HandleClick();
    }

    private void TryPop()
    {
        var active = 0;
        for (var i = 0; i < _slots.Length; i++)
            if (_slots[i].Active)
                active++;
        if (active >= maxActive)
            return;

        var candidates = new int[_slots.Length];
        var count = 0;
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].Active)
                continue;
            candidates[count++] = i;
        }
        if (count == 0)
            return;

        var idx = candidates[Random.Range(0, count)];
        var hole = holes[idx];
        var go = SpawnHazard(hole.position + Vector3.up * 0.15f);
        _slots[idx] = new Slot
        {
            Hazard = go.transform,
            Life = upSeconds,
            Active = true
        };
    }

    private void UpdateSlots()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active || _slots[i].Hazard == null)
                continue;

            var slot = _slots[i];
            slot.Life -= Time.deltaTime;
            var t = Mathf.Clamp01(slot.Life / upSeconds);
            // Pop up then sink: scale peaks mid-life.
            var pop = Mathf.Sin(t * Mathf.PI);
            slot.Hazard.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.95f, pop);
            _slots[i] = slot;

            if (slot.Life > 0f)
                continue;

            Destroy(slot.Hazard.gameObject);
            _slots[i] = default;
            _hits = Mathf.Max(0, _hits - 1);
            UpdateProgress();
        }
    }

    private void HandleClick()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        for (var i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active || _slots[i].Hazard == null)
                continue;
            if (Vector2.Distance(world, _slots[i].Hazard.position) > hitRadius)
                continue;

            Destroy(_slots[i].Hazard.gameObject);
            _slots[i] = default;
            _hits++;
            UpdateProgress();
            if (_hits >= requiredHits)
                Complete();
            return;
        }
    }

    private void UpdateProgress()
    {
        if (progressFill == null)
            return;
        var t = requiredHits <= 0 ? 1f : (float)_hits / requiredHits;
        var w = Mathf.Max(0.08f, t * progressMaxWidth);
        progressFill.localScale = new Vector3(w, _progressBaseScale.y, 1f);
        progressFill.localPosition = new Vector3(_progressBasePos.x + (w - _progressBaseScale.x) * 0.5f,
            _progressBasePos.y, _progressBasePos.z);
    }

    private GameObject SpawnHazard(Vector3 pos)
    {
        GameObject go;
        if (hazardPrefab != null)
        {
            go = Instantiate(hazardPrefab, pos, Quaternion.identity, transform);
            go.transform.localScale = Vector3.one * 0.4f;
        }
        else
        {
            go = new GameObject("Hazard");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.4f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.95f, 0.35f, 0.25f);
            sr.sortingOrder = 5;
        }

        go.SetActive(true);
        var colorSr = MiniGameVisuals.FindSprite(go.transform);
        if (colorSr != null)
            colorSr.color = new Color(0.95f, 0.35f, 0.25f);
        return go;
    }

    private void EnsureHoles()
    {
        if (holes != null && holes.Length >= 6)
        {
            var ok = true;
            for (var i = 0; i < holes.Length; i++)
                if (holes[i] == null)
                    ok = false;
            if (ok)
                return;
        }

        holes = new Transform[6];
        for (var i = 0; i < 6; i++)
        {
            var col = i % 3;
            var row = i / 3;
            var go = new GameObject($"Hole{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(-2.6f + col * 2.6f, 1.1f - row * 2.2f, 0f);
            go.transform.localScale = new Vector3(1.4f, 0.55f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.15f, 0.12f, 0.1f);
            sr.sortingOrder = 1;
            holes[i] = go.transform;
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
