using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HappyEndingFadeManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup whiteFadeCanvas;

    private Coroutine _fadeRoutine;

    public void SetWhiteAlpha(float alpha)
    {
        if (whiteFadeCanvas == null)
            return;

        whiteFadeCanvas.alpha = Mathf.Clamp01(alpha);
        whiteFadeCanvas.blocksRaycasts = whiteFadeCanvas.alpha > 0.01f;
    }

    public Coroutine FadeFromWhite(float duration)
    {
        return StartFade(1f, 0f, duration);
    }

    public Coroutine FadeToWhite(float duration)
    {
        return StartFade(0f, 1f, duration);
    }

    private Coroutine StartFade(float from, float to, float duration)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(Fade(from, to, duration));
        return _fadeRoutine;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (whiteFadeCanvas == null)
            yield break;

        SetWhiteAlpha(from);
        var elapsed = 0f;
        duration = Mathf.Max(duration, 0.01f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetWhiteAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetWhiteAlpha(to);
        _fadeRoutine = null;
    }
}
