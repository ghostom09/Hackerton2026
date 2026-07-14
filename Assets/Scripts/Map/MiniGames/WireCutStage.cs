using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class WireCutStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] wires;
    [SerializeField] private SpriteRenderer hintRenderer;

    [Header("Cut Effect")]
    [Tooltip("전선이 가운데에서 분리되는 데 걸리는 시간입니다.")]
    [SerializeField] private float cutAnimationDuration = 0.18f;
    [Tooltip("잘린 전선 두 조각 사이의 간격입니다.")]
    [SerializeField] private float cutGap = 0.22f;
    [Tooltip("잘린 전선 조각이 아래로 처지는 거리입니다.")]
    [SerializeField] private float cutDrop = 0.06f;

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

        // Start each round with a random wire, rather than the prefab's authored IsSafe value.
        SelectSafeWire(RandomWireIndex());
    }

    private void SelectNextSafeWire(int incorrectIndex)
    {
        // Do not immediately select either the wire that was just clicked or the old answer.
        var nextIndex = RandomWireIndex(incorrectIndex, _safeIndex);
        if (nextIndex >= 0)
            SelectSafeWire(nextIndex);
    }

    private int RandomWireIndex(params int[] excludedIndices)
    {
        var candidates = new System.Collections.Generic.List<int>();
        for (var i = 0; i < wires.Length; i++)
        {
            if (wires[i] == null || System.Array.IndexOf(excludedIndices, i) >= 0)
                continue;

            candidates.Add(i);
        }

        // Small or incomplete wire sets may leave no non-excluded candidate.
        if (candidates.Count == 0)
        {
            for (var i = 0; i < wires.Length; i++)
            {
                if (wires[i] != null)
                    candidates.Add(i);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : -1;
    }

    private void SelectSafeWire(int safeIndex)
    {
        _safeIndex = safeIndex;
        for (var i = 0; i < wires.Length; i++)
        {
            if (wires[i] != null)
                wires[i].IsSafe = i == _safeIndex;
        }

        UpdateHint();
    }

    private void UpdateHint()
    {
        if (hintRenderer != null && _safeIndex >= 0 && wires[_safeIndex] != null)
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
                StartCoroutine(CutWireAndComplete(wires[i]));
            }
            else
            {
                SelectNextSafeWire(i);
            }
            return;
        }
    }

    private IEnumerator CutWireAndComplete(MiniGameTarget wire)
    {
        _complete = true;

        var source = MiniGameVisuals.FindSprite(wire);
        if (source == null || source.sprite == null)
        {
            RequestNext();
            yield break;
        }

        var sourceTransform = source.transform;
        var sourcePosition = sourceTransform.localPosition;
        var sourceScale = sourceTransform.localScale;
        var pieceCenterOffset = source.sprite.bounds.size.x * Mathf.Abs(sourceScale.x) * 0.25f;
        var leftPiece = CreateCutPiece(source, "CutLeft", sourcePosition + Vector3.left * pieceCenterOffset);
        var rightPiece = CreateCutPiece(source, "CutRight", sourcePosition + Vector3.right * pieceCenterOffset);
        source.enabled = false;

        var leftStart = leftPiece.localPosition;
        var rightStart = rightPiece.localPosition;
        var separation = Mathf.Max(0f, cutGap) * 0.5f;
        var drop = Mathf.Max(0f, cutDrop);
        var leftEnd = leftStart + new Vector3(-separation, -drop, 0f);
        var rightEnd = rightStart + new Vector3(separation, -drop, 0f);
        var duration = Mathf.Max(0f, cutAnimationDuration);

        if (duration > 0f)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                leftPiece.localPosition = Vector3.Lerp(leftStart, leftEnd, progress);
                rightPiece.localPosition = Vector3.Lerp(rightStart, rightEnd, progress);
                yield return null;
            }
        }

        leftPiece.localPosition = leftEnd;
        rightPiece.localPosition = rightEnd;
        RequestNext();
    }

    private static Transform CreateCutPiece(SpriteRenderer source, string name, Vector3 localPosition)
    {
        var piece = new GameObject(name).transform;
        piece.SetParent(source.transform.parent, false);
        piece.localPosition = localPosition;
        piece.localRotation = source.transform.localRotation;

        var sourceScale = source.transform.localScale;
        piece.localScale = new Vector3(sourceScale.x * 0.5f, sourceScale.y, sourceScale.z);

        var renderer = piece.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = source.color;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.sharedMaterial = source.sharedMaterial;
        return piece;
    }

    private void RequestNext()
    {
        MiniGameClear.RequestNext();
    }
}
