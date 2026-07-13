using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class WireCutStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] wires;
    [SerializeField] private SpriteRenderer hintRenderer;

    private bool _complete;
    private int _safeIndex = -1;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (wires == null || wires.Length == 0)
        {
            var list = new System.Collections.Generic.List<MiniGameTarget>();
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Wire"))
                    continue;
                var target = child.GetComponent<MiniGameTarget>();
                if (target != null)
                    list.Add(target);
            }
            wires = list.ToArray();
        }
        if (hintRenderer == null)
        {
            var hint = transform.Find("Hint");
            if (hint != null)
                hintRenderer = MiniGameVisuals.FindSprite(hint);
        }
        PickSafeWire();
    }

    private void PickSafeWire()
    {
        if (wires == null || wires.Length == 0)
            return;

        for (var i = 0; i < wires.Length; i++)
        {
            if (wires[i] != null && wires[i].IsSafe)
            {
                _safeIndex = i;
                break;
            }
        }

        if (_safeIndex < 0)
        {
            _safeIndex = Random.Range(0, wires.Length);
            for (var i = 0; i < wires.Length; i++)
            {
                if (wires[i] != null)
                    wires[i].IsSafe = i == _safeIndex;
            }
        }

        if (hintRenderer != null && wires[_safeIndex] != null)
        {
            var wireRenderer = MiniGameVisuals.FindSprite(wires[_safeIndex]);
            if (wireRenderer != null)
                hintRenderer.color = wireRenderer.color;
        }
    }

    private void Update()
    {
        if (_complete || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (wires == null)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        for (var i = 0; i < wires.Length; i++)
        {
            if (wires[i] == null || !wires[i].Contains(world))
                continue;

            if (i == _safeIndex)
            {
                var visual = wires[i].transform.Find("Visual");
                if (visual != null)
                {
                    var scale = visual.localScale;
                    visual.localScale = new Vector3(scale.x, Mathf.Min(0.08f, scale.y), 1f);
                }
                else
                {
                    var scale = wires[i].transform.localScale;
                    wires[i].transform.localScale = new Vector3(scale.x, 0.08f, 1f);
                }
                Complete();
            }
            else
            {
                wires[i].transform.localPosition += new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0f);
                if (hintRenderer != null)
                    hintRenderer.color = Color.red;
            }
            return;
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
