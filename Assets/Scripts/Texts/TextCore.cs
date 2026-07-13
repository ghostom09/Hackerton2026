using System.Collections;
using UnityEngine;

public class TextCore : MonoBehaviour
{
    private TextReader _textReader;
    private TMPRenderer _tmpRenderer;

    private Coroutine _textFlowCoroutine;
    private string _currentRawText;

    public bool IsPlaying => _textFlowCoroutine != null;

    private void Awake()
    {
        _textReader = GetComponent<TextReader>();
        _tmpRenderer = GetComponent<TMPRenderer>();
    }

    [ContextMenu("Play Example")]
    public void PlayExample()
    {
        PlayText("<type charDelay=0.1>Hello, <wave>world</wave>! <shake intensity=2>Shake</shake> <rainbow>Rainbow</rainbow></type>");
    }

    public void PlayText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        StopTextFlow();
        _currentRawText = text;
        _textFlowCoroutine = StartCoroutine(SetTextFlowCoroutine(text));
    }

    public void CompleteText()
    {
        StopTextFlow();

        if (string.IsNullOrEmpty(_currentRawText))
            return;

        ApplyText(_currentRawText, instant: true);
    }

    public void ClearText()
    {
        StopTextFlow();
        _currentRawText = null;

        if (_tmpRenderer == null)
            return;

        _tmpRenderer.ClearEffects();
        _tmpRenderer.SetText(string.Empty);
    }

    private void OnDestroy()
    {
        StopTextFlow();
    }

    private void StopTextFlow()
    {
        if (_textFlowCoroutine == null)
            return;

        StopCoroutine(_textFlowCoroutine);
        _textFlowCoroutine = null;
    }

    private IEnumerator SetTextFlowCoroutine(string text)
    {
        yield return null;

        ApplyText(text, instant: false);
        _textFlowCoroutine = null;
    }

    private void ApplyText(string text, bool instant)
    {
        if (_textReader == null || _tmpRenderer == null)
        {
            Debug.LogError("[TextCore] TextReader or TMPRenderer is not assigned.");
            return;
        }

        _textReader.Read(text);

        _tmpRenderer.ClearEffects();
        _tmpRenderer.SetText(_textReader.OutputText);
        _tmpRenderer.RefreshCharContexts();

        var startTime = instant ? Time.time - 100000f : Time.time;

        foreach (var range in _textReader.effectRanges)
        {
            if (!TextEffectRegistry.Effects.TryGetValue(range.name, out var factory))
            {
                Debug.LogWarning($"[TextCore] Unknown effect: {range.name}");
                continue;
            }

            var effect = factory(range.attributes);

            if (effect is TypingEffect typing)
            {
                typing.RangeStart = range.startIndex;
                typing.StartTime = startTime;
            }

            for (var i = range.startIndex; i <= range.endIndex; i++)
                _tmpRenderer.RegisterEffect(i, effect);
        }

        if (!instant)
            return;

        for (var i = 0; i < _textReader.OutputText.Length; i++)
            _tmpRenderer.SetAppearTime(i, startTime);
    }
}
