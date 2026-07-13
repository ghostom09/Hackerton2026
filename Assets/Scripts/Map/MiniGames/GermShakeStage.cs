using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shake the mouse hard to fling germs off a contaminated surface.
/// </summary>
[DisallowMultipleComponent]
public class GermShakeStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform surface;
    [SerializeField] private Transform germRoot;
    [SerializeField] private Transform progressFill;
    [SerializeField] private GameObject germPrefab;

    [Header("Rules")]
    [SerializeField] private int germCount = 10;
    [SerializeField] private float shakeToPop = 18f;
    [SerializeField] private float shakeDecay = 14f;
    [SerializeField] private float shakeVisual = 0.18f;
    [SerializeField] private float ejectSpeed = 7f;
    [SerializeField] private float progressMaxWidth = 4f;

    private readonly List<Transform> _germs = new();
    private readonly List<Transform> _flying = new();
    private readonly List<Vector2> _flyVel = new();
    private float _shake;
    private float _popBank;
    private int _cleared;
    private int _total;
    private bool _complete;
    private Vector3 _surfaceBase;
    private Vector3 _progressBasePos;
    private Vector3 _progressBaseScale;

    private void Awake()
    {
        if (germRoot == null)
            germRoot = transform;
        if (surface != null)
            _surfaceBase = surface.localPosition;
        if (progressFill != null)
        {
            _progressBasePos = progressFill.localPosition;
            _progressBaseScale = progressFill.localScale;
        }

        SpawnGerms();
        UpdateProgress();
    }

    private void Update()
    {
        if (_complete)
            return;

        AccumulateShake();
        WobbleSurface();
        TryPopGerms();
        UpdateFlying();
    }

    private void AccumulateShake()
    {
        var add = 0f;
        if (Mouse.current != null)
            add += Mouse.current.delta.ReadValue().magnitude * 0.085f;

        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame ||
                k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame ||
                k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame ||
                k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame)
                add += 6.5f;
        }

        _shake = Mathf.Clamp(_shake + add - shakeDecay * Time.deltaTime, 0f, shakeToPop * 2.5f);
        _popBank += add;
    }

    private void WobbleSurface()
    {
        if (surface == null)
            return;

        var t = Mathf.Clamp01(_shake / shakeToPop);
        var ox = Mathf.Sin(Time.time * 55f) * shakeVisual * t;
        var oy = Mathf.Cos(Time.time * 48f) * shakeVisual * t * 0.7f;
        surface.localPosition = _surfaceBase + new Vector3(ox, oy, 0f);
        surface.localRotation = Quaternion.Euler(0f, 0f, ox * 25f);
    }

    private void TryPopGerms()
    {
        while (_popBank >= shakeToPop && _germs.Count > 0)
        {
            _popBank -= shakeToPop;
            EjectGerm(Random.Range(0, _germs.Count));
        }
    }

    private void EjectGerm(int index)
    {
        var germ = _germs[index];
        _germs.RemoveAt(index);
        if (germ == null)
            return;

        var dir = ((Vector2)germ.position).normalized;
        if (dir.sqrMagnitude < 0.01f)
            dir = Random.insideUnitCircle.normalized;
        dir = (dir + Random.insideUnitCircle * 0.5f).normalized;

        germ.SetParent(transform, true);
        _flying.Add(germ);
        _flyVel.Add(dir * ejectSpeed + Vector2.up * Random.Range(1.5f, 3.5f));
        _cleared++;
        UpdateProgress();

        if (_cleared >= _total)
            Complete();
    }

    private void UpdateFlying()
    {
        for (var i = _flying.Count - 1; i >= 0; i--)
        {
            var g = _flying[i];
            if (g == null)
            {
                _flying.RemoveAt(i);
                _flyVel.RemoveAt(i);
                continue;
            }

            _flyVel[i] += Vector2.down * (14f * Time.deltaTime);
            g.position += (Vector3)(_flyVel[i] * Time.deltaTime);
            g.Rotate(0f, 0f, 420f * Time.deltaTime);

            if (g.position.y < -5.5f || Mathf.Abs(g.position.x) > 8f)
            {
                Destroy(g.gameObject);
                _flying.RemoveAt(i);
                _flyVel.RemoveAt(i);
            }
        }
    }

    private void SpawnGerms()
    {
        for (var i = germRoot.childCount - 1; i >= 0; i--)
            Destroy(germRoot.GetChild(i).gameObject);
        _germs.Clear();

        _total = germCount;
        for (var i = 0; i < germCount; i++)
        {
            var ang = i * Mathf.PI * 2f / germCount + Random.Range(-0.15f, 0.15f);
            var rad = Random.Range(0.55f, 1.55f);
            var local = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad, 0f);
            var go = SpawnGerm(local);
            _germs.Add(go.transform);
        }
    }

    private GameObject SpawnGerm(Vector3 localPos)
    {
        GameObject go;
        if (germPrefab != null)
        {
            go = Instantiate(germPrefab, germRoot);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * Random.Range(0.35f, 0.55f);
        }
        else
        {
            go = new GameObject("Germ");
            go.transform.SetParent(germRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * Random.Range(0.35f, 0.55f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.45f, 0.9f, 0.35f);
            sr.sortingOrder = 5;
        }

        go.name = "Germ";
        go.SetActive(true);
        var colorSr = MiniGameVisuals.FindSprite(go.transform);
        if (colorSr != null)
            colorSr.color = new Color(0.45f, 0.9f, 0.35f);
        return go;
    }

    private void UpdateProgress()
    {
        if (progressFill == null)
            return;
        var t = _total <= 0 ? 1f : (float)_cleared / _total;
        var w = Mathf.Max(0.08f, t * progressMaxWidth);
        progressFill.localScale = new Vector3(w, _progressBaseScale.y, 1f);
        progressFill.localPosition = new Vector3(
            _progressBasePos.x + (w - _progressBaseScale.x) * 0.5f,
            _progressBasePos.y,
            _progressBasePos.z);
    }

    private void Complete()
    {
        _complete = true;
        if (surface != null)
        {
            surface.localPosition = _surfaceBase;
            surface.localRotation = Quaternion.identity;
        }
        MiniGameClear.RequestNext();
    }
}
