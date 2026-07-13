using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Knife Hit: stick knives into a spinning log. Hitting an existing knife bounces you out and resets.
/// </summary>
[DisallowMultipleComponent]
public class KnifeHitStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform log;
    [SerializeField] private Transform readyKnife;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject knifePrefab;

    [Header("Rules")]
    [SerializeField] private int requiredKnives = 8;
    [SerializeField] private float spinSpeed = 110f;
    [SerializeField] private float spinAccel = 4f;
    [SerializeField] private float logRadius = 1.55f;
    [SerializeField] private float knifeSpeed = 22f;
    [SerializeField] private float collideDegrees = 18f;
    [SerializeField] private float knifeLength = 0.95f;
    [SerializeField] private float readyY = -3.2f;

    private readonly List<float> _stuckAngles = new();
    private readonly List<Transform> _stuckKnives = new();
    private Transform _flying;
    private bool _complete;
    private bool _bouncing;
    private Vector2 _bounceVel;
    private float _bounceLife;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;
        if (readyKnife != null)
            readyKnife.position = new Vector3(0f, readyY, 0f);
    }

    private void Update()
    {
        if (_complete || log == null)
            return;

        spinSpeed += spinAccel * Time.deltaTime;
        log.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        SyncStuckKnives();

        if (_bouncing)
        {
            UpdateBounce();
            return;
        }

        if (_flying != null)
        {
            UpdateFlying();
            return;
        }

        if (WasFirePressed())
            Fire();
    }

    private static bool WasFirePressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame);
    }

    private void Fire()
    {
        if (readyKnife != null)
            readyKnife.gameObject.SetActive(false);

        var go = SpawnKnife(new Vector3(0f, readyY, 0f), "FlyingKnife");
        _flying = go.transform;
        _flying.localRotation = Quaternion.identity;
    }

    private void UpdateFlying()
    {
        _flying.position += Vector3.up * (knifeSpeed * Time.deltaTime);
        var dist = Vector2.Distance(_flying.position, log.position);
        if (dist > logRadius + 0.05f)
            return;

        var dir = ((Vector2)_flying.position - (Vector2)log.position).normalized;
        var absAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var relative = Mathf.DeltaAngle(0f, absAngle - log.eulerAngles.z);

        if (HitsStuck(relative))
        {
            StartBounce(dir);
            return;
        }

        StickKnife(relative);
        Destroy(_flying.gameObject);
        _flying = null;

        if (readyKnife != null)
            readyKnife.gameObject.SetActive(true);

        if (_stuckAngles.Count >= requiredKnives)
            Complete();
    }

    private bool HitsStuck(float relative)
    {
        for (var i = 0; i < _stuckAngles.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(relative, _stuckAngles[i])) < collideDegrees)
                return true;
        }
        return false;
    }

    private void StickKnife(float relative)
    {
        _stuckAngles.Add(relative);
        var go = SpawnKnife(log.position, $"StuckKnife_{_stuckAngles.Count}");
        _stuckKnives.Add(go.transform);
        PlaceStuck(go.transform, relative);
    }

    private void SyncStuckKnives()
    {
        for (var i = 0; i < _stuckKnives.Count; i++)
        {
            if (_stuckKnives[i] == null)
                continue;
            PlaceStuck(_stuckKnives[i], _stuckAngles[i]);
        }
    }

    private void PlaceStuck(Transform knife, float relative)
    {
        var abs = log.eulerAngles.z + relative;
        var rad = abs * Mathf.Deg2Rad;
        var tip = (Vector2)log.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * logRadius;
        var outward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        knife.position = tip + outward * (knifeLength * 0.35f);
        knife.localRotation = Quaternion.Euler(0f, 0f, abs - 90f);
    }

    private void StartBounce(Vector2 hitDir)
    {
        _bouncing = true;
        _bounceVel = (-hitDir + Vector2.down * 0.6f).normalized * 10f;
        _bounceLife = 0.55f;
        // Keep flying knife as bounce visual.
    }

    private void UpdateBounce()
    {
        if (_flying != null)
        {
            _flying.position += (Vector3)(_bounceVel * Time.deltaTime);
            _bounceVel += Vector2.down * (18f * Time.deltaTime);
            _flying.Rotate(0f, 0f, 720f * Time.deltaTime);
        }

        _bounceLife -= Time.deltaTime;
        if (_bounceLife > 0f)
            return;

        ResetBoard();
    }

    private void ResetBoard()
    {
        if (_flying != null)
        {
            Destroy(_flying.gameObject);
            _flying = null;
        }

        for (var i = 0; i < _stuckKnives.Count; i++)
        {
            if (_stuckKnives[i] != null)
                Destroy(_stuckKnives[i].gameObject);
        }

        _stuckKnives.Clear();
        _stuckAngles.Clear();
        _bouncing = false;

        if (readyKnife != null)
            readyKnife.gameObject.SetActive(true);
    }

    private GameObject SpawnKnife(Vector3 pos, string name)
    {
        GameObject go;
        if (knifePrefab != null)
        {
            go = Instantiate(knifePrefab, pos, Quaternion.identity, spawnParent);
            go.transform.localScale = new Vector3(0.22f, knifeLength, 1f);
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(spawnParent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.22f, knifeLength, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.85f, 0.85f, 0.9f);
            sr.sortingOrder = 6;
        }

        go.name = name;
        go.SetActive(true);
        var knifeSr = MiniGameVisuals.FindSprite(go.transform);
        if (knifeSr != null)
            knifeSr.color = new Color(0.85f, 0.85f, 0.9f);
        return go;
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
