using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CprRhythmStage : MonoBehaviour
{
    private const float MistimedPressPenaltySeconds = 1f;

    [Header("Refs")]
    [SerializeField] private Transform needle;
    [SerializeField] private Transform barLeft;
    [SerializeField] private Transform barRight;
    [SerializeField] private Transform zone;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject goodFlashPrefab;
    [SerializeField] private GameObject badFlashPrefab;

    [Header("Heartbeat Visuals")]
    [SerializeField] private Transform heart;
    [SerializeField] private Transform visual2;
    [SerializeField, Min(1f)] private float heartBeatsPerMinute = 72f;
    [SerializeField, Range(1f, 1.5f)] private float heartPulseScale = 1.12f;
    [SerializeField, Range(1f, 1.5f)] private float pressPulseScale = 1.1f;
    [SerializeField, Min(.01f)] private float pressPulseDuration = .18f;

    [Header("Rules")]
    [SerializeField] private int requiredHits = 6;
    [SerializeField] private float needleSpeed = 2.4f;
    [SerializeField] private float zoneMin = 0.62f;
    [SerializeField] private float zoneMax = 0.82f;

    private float _t;
    private int _hits;
    private bool _complete;
    private Vector3 _heartBaseScale;
    private Vector3 _visual2BaseScale;
    private Coroutine _pressPulseRoutine;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;

        heart ??= FindChild(transform, "Chest");
        visual2 ??= FindChild(transform, "Visual2");
        if (heart != null) _heartBaseScale = heart.localScale;
        if (visual2 != null) _visual2BaseScale = visual2.localScale;
    }

    private void Update()
    {
        if (_complete || needle == null)
            return;

        _t += Time.deltaTime * needleSpeed;
        AnimateHeartbeat();
        var x = Mathf.PingPong(_t, 1f);
        var left = barLeft != null ? barLeft.position.x : -3.2f;
        var right = barRight != null ? barRight.position.x : 3.2f;
        needle.position = new Vector3(Mathf.Lerp(left, right, x), needle.position.y, needle.position.z);

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (visual2 != null)
        {
            if (_pressPulseRoutine != null) StopCoroutine(_pressPulseRoutine);
            _pressPulseRoutine = StartCoroutine(PulsePressVisual());
        }

        if (x >= zoneMin && x <= zoneMax)
        {
            _hits++;
            SpawnFlash(true);
            if (_hits >= requiredHits)
                Complete();
        }
        else
        {
            SpawnFlash(false);
            _hits = Mathf.Max(0, _hits - 1);
            GameManager.Instance?.ReduceCurrentMapTime(MistimedPressPenaltySeconds);
        }
    }

    private void SpawnFlash(bool good)
    {
        var prefab = good ? goodFlashPrefab : badFlashPrefab;
        if (prefab == null)
            return;
        var pos = zone != null ? zone.position + Vector3.up * 1.6f : new Vector3(0f, 1.6f, 0f);
        var flash = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
        flash.SetActive(true);
        Destroy(flash, 0.15f);
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }

    private void AnimateHeartbeat()
    {
        if (heart == null) return;

        float beatsPerSecond = heartBeatsPerMinute / 60f;
        float beat = Mathf.Pow((Mathf.Sin(Time.time * beatsPerSecond * Mathf.PI * 2f) + 1f) * .5f, 3f);
        heart.localScale = Vector3.Lerp(_heartBaseScale, _heartBaseScale * heartPulseScale, beat);
    }

    private IEnumerator PulsePressVisual()
    {
        float halfDuration = pressPulseDuration * .5f;
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            visual2.localScale = Vector3.Lerp(_visual2BaseScale, _visual2BaseScale * pressPulseScale, elapsed / halfDuration);
            yield return null;
        }
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            visual2.localScale = Vector3.Lerp(_visual2BaseScale * pressPulseScale, _visual2BaseScale, elapsed / halfDuration);
            yield return null;
        }

        visual2.localScale = _visual2BaseScale;
        _pressPulseRoutine = null;
    }

    private static Transform FindChild(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == targetName)
                return child;
        return null;
    }
}
