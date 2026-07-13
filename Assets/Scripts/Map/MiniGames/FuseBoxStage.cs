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

    private readonly List<int> _sequence = new();
    private readonly List<int> _playerInput = new();
    private readonly Color[] _baseColors = new Color[8];
    private bool _inputEnabled;
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
        StartCoroutine(ShowSequenceRoutine());
    }

    private void CacheBaseColors()
    {
        if (fuses == null)
            return;
        for (var i = 0; i < fuses.Length && i < _baseColors.Length; i++)
        {
            if (fuses[i] == null)
                continue;
            var renderer = MiniGameVisuals.FindSprite(fuses[i]);
            _baseColors[i] = renderer != null ? renderer.color : Color.white;
        }
    }

    private void Update()
    {
        if (_complete || !_inputEnabled || _showing || Mouse.current == null)
            return;
        if (!Mouse.current.leftButton.wasPressedThisFrame || fuses == null)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        for (var i = 0; i < fuses.Length; i++)
        {
            if (fuses[i] == null || !fuses[i].Contains(world))
                continue;

            StartCoroutine(FlashFuse(i, inputFlashDuration));
            _playerInput.Add(i);

            if (_playerInput[_playerInput.Count - 1] != _sequence[_playerInput.Count - 1])
            {
                _playerInput.Clear();
                StartCoroutine(ShowSequenceRoutine());
                return;
            }

            if (_playerInput.Count >= _sequence.Count)
                Complete();
            return;
        }
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
        if (fuses == null || index < 0 || index >= fuses.Length || fuses[index] == null)
            yield break;

        var renderer = MiniGameVisuals.FindSprite(fuses[index]);
        if (renderer == null)
            yield break;

        renderer.color = Color.white;
        yield return new WaitForSeconds(duration);
        if (renderer != null)
            renderer.color = _baseColors[index];
    }

    private void Complete()
    {
        _complete = true;
        _inputEnabled = false;
        MiniGameClear.RequestNext();
    }
}
