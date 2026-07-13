using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CprRhythmStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform needle;
    [SerializeField] private Transform barLeft;
    [SerializeField] private Transform barRight;
    [SerializeField] private Transform zone;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject goodFlashPrefab;
    [SerializeField] private GameObject badFlashPrefab;

    [Header("Rules")]
    [SerializeField] private int requiredHits = 6;
    [SerializeField] private float needleSpeed = 2.4f;
    [SerializeField] private float zoneMin = 0.62f;
    [SerializeField] private float zoneMax = 0.82f;

    private float _t;
    private int _hits;
    private bool _complete;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;
    }

    private void Update()
    {
        if (_complete || needle == null)
            return;

        _t += Time.deltaTime * needleSpeed;
        var x = Mathf.PingPong(_t, 1f);
        var left = barLeft != null ? barLeft.position.x : -3.2f;
        var right = barRight != null ? barRight.position.x : 3.2f;
        needle.position = new Vector3(Mathf.Lerp(left, right, x), needle.position.y, needle.position.z);

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

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
}
