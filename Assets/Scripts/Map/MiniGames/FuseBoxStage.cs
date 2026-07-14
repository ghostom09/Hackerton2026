using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FuseBoxStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] fuses;

    [Header("Rules")]
    [SerializeField] private int sequenceLength = 4;
    [SerializeField] private float showFlashDuration = 0.35f;
    [SerializeField] private float inputFlashDuration = 0.18f;
    [Tooltip("순서를 한 번 틀렸을 때 현재 스테이지 타이머에서 차감할 시간(초)입니다.")]
    [SerializeField, Min(0f)] private float wrongSequenceTimePenalty = 1f;

    private readonly List<int> _sequence = new();
    private readonly List<int> _playerInput = new();
    private readonly Queue<int> _pendingInput = new();
    private readonly Dictionary<SpriteRenderer, Color> _baseColors = new();
    private readonly Dictionary<SpriteRenderer, Material> _baseMaterials = new();
    private int[] _flashVersions = System.Array.Empty<int>();
    private Material _whiteFlashMaterial;
    private bool _inputEnabled;
    private bool _processingInput;
    private bool _complete;
    private bool _showing;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (fuses == null || fuses.Length == 0)
        {
            var list = new System.Collections.Generic.List<MiniGameTarget>();
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Fuse"))
                    continue;
                var target = child.GetComponent<MiniGameTarget>();
                if (target != null)
                    list.Add(target);
            }
            fuses = list.ToArray();
        }
        CacheBaseColors();
        _flashVersions = new int[fuses != null ? fuses.Length : 0];
        CreateWhiteFlashMaterial();
        StartCoroutine(ShowSequenceRoutine());
    }

    private void OnDestroy()
    {
        if (_whiteFlashMaterial != null)
            Destroy(_whiteFlashMaterial);
    }

    private void CreateWhiteFlashMaterial()
    {
        var shader = Resources.Load<Shader>("FuseWhiteSilhouette");
        if (shader == null)
            shader = Shader.Find("FuseBox/White Silhouette");
        if (shader != null)
            _whiteFlashMaterial = new Material(shader);
    }

    private void CacheBaseColors()
    {
        _baseColors.Clear();
        _baseMaterials.Clear();
        if (fuses == null)
            return;
        for (var i = 0; i < fuses.Length; i++)
        {
            if (fuses[i] == null)
                continue;
            foreach (var renderer in fuses[i].GetComponentsInChildren<SpriteRenderer>(true))
            {
                _baseColors[renderer] = renderer.color;
                _baseMaterials[renderer] = renderer.sharedMaterial;
            }
        }
    }

    private void Update()
    {
        if (_complete || !_inputEnabled || _showing || Mouse.current == null)
            return;
        if (!Mouse.current.leftButton.wasPressedThisFrame || fuses == null)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        if (!TryGetClickedFuse(world, out var fuseIndex))
            return;

        // Store every press immediately. This prevents rapid repeated clicks
        // (including the same fuse twice) from being lost during the flash.
        _pendingInput.Enqueue(fuseIndex);
        if (!_processingInput)
            StartCoroutine(ProcessPlayerInputQueue());
    }

    private bool TryGetClickedFuse(Vector3 world, out int clickedIndex)
    {
        clickedIndex = -1;
        if (fuses == null)
            return false;

        var closestDistance = float.MaxValue;
        for (var i = 0; i < fuses.Length; i++)
        {
            var fuse = fuses[i];
            if (fuse == null)
                continue;

            var hasVisibleRenderer = false;
            foreach (var renderer in fuse.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                    continue;

                hasVisibleRenderer = true;
                var bounds = renderer.bounds;
                if (world.x < bounds.min.x || world.x > bounds.max.x ||
                    world.y < bounds.min.y || world.y > bounds.max.y)
                    continue;

                var distance = ((Vector2)bounds.center - (Vector2)world).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                clickedIndex = i;
            }

            // Keep a no-art fallback for incomplete prefabs, but never let an
            // invisible root box overlap and steal a visible fuse click.
            if (!hasVisibleRenderer && fuse.Contains(world))
            {
                var distance = ((Vector2)fuse.transform.position - (Vector2)world).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    clickedIndex = i;
                }
            }
        }

        return clickedIndex >= 0;
    }

    private IEnumerator ProcessPlayerInputQueue()
    {
        _processingInput = true;

        while (_pendingInput.Count > 0 && !_complete)
        {
            var index = _pendingInput.Dequeue();

            // Show the click feedback before validating the sequence, then
            // return the fuse to its original colour for the next input.
            var flashVersion = BeginFuseFlash(index);
            yield return new WaitForSeconds(inputFlashDuration);
            RestoreFuseColor(index, flashVersion);

            _playerInput.Add(index);
            var inputPosition = _playerInput.Count - 1;
            if (inputPosition >= _sequence.Count || _playerInput[inputPosition] != _sequence[inputPosition])
            {
                _pendingInput.Clear();
                _playerInput.Clear();
                _inputEnabled = false;
                GameManager.Instance?.ReduceCurrentMapTime(wrongSequenceTimePenalty);
                yield return ShowSequenceRoutine();
                _processingInput = false;
                yield break;
            }

            if (_playerInput.Count >= _sequence.Count)
            {
                _pendingInput.Clear();
                _processingInput = false;
                Complete();
                yield break;
            }
        }

        _processingInput = false;
    }

    private IEnumerator ShowSequenceRoutine()
    {
        _showing = true;
        _inputEnabled = false;
        _playerInput.Clear();

        if (_sequence.Count == 0 && fuses != null && fuses.Length > 0)
        {
            for (var i = 0; i < sequenceLength; i++)
                _sequence.Add(Random.Range(0, fuses.Length));
        }

        yield return new WaitForSeconds(0.45f);

        for (var i = 0; i < _sequence.Count; i++)
        {
            yield return FlashFuse(_sequence[i], showFlashDuration);
            yield return new WaitForSeconds(0.18f);
        }

        _showing = false;
        _inputEnabled = true;
    }

    private IEnumerator FlashFuse(int index, float duration)
    {
        var flashVersion = BeginFuseFlash(index);
        if (flashVersion < 0)
            yield break;

        yield return new WaitForSeconds(duration);
        RestoreFuseColor(index, flashVersion);
    }

    private int BeginFuseFlash(int index)
    {
        if (fuses == null || index < 0 || index >= fuses.Length || fuses[index] == null)
            return -1;

        var renderers = fuses[index].GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
            return -1;

        var flashVersion = ++_flashVersions[index];
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                // A white tint does not whiten a coloured PNG. This material
                // keeps the image alpha but renders every visible pixel white.
                if (_whiteFlashMaterial != null)
                    renderer.sharedMaterial = _whiteFlashMaterial;
                renderer.color = Color.white;
            }
        }
        return flashVersion;
    }

    private void RestoreFuseColor(int index, int flashVersion)
    {
        if (fuses == null || index < 0 || index >= fuses.Length || fuses[index] == null)
            return;
        if (index >= _flashVersions.Length)
            return;
        if (_flashVersions[index] != flashVersion)
            return;

        foreach (var renderer in fuses[index].GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
                continue;
            if (_baseMaterials.TryGetValue(renderer, out var baseMaterial))
                renderer.sharedMaterial = baseMaterial;
            if (_baseColors.TryGetValue(renderer, out var baseColor))
                renderer.color = baseColor;
        }
    }

    private void Complete()
    {
        _complete = true;
        _inputEnabled = false;
        MiniGameClear.RequestNext();
    }
}
