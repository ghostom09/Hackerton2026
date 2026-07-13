using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HappyEndingFadeManager : MonoBehaviour
{
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private Image whiteFadeImage;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        SetBlackAlpha(1f);
        SetWhiteAlpha(0f);
    }

    public void SetBlackAlpha(float alpha)
    {
        SetImageAlpha(blackFadeImage, Color.black, alpha);
    }

    public void SetWhiteAlpha(float alpha)
    {
        SetImageAlpha(whiteFadeImage, Color.white, alpha);
    }

    public Coroutine FadeFromBlack(float duration)
    {
        return StartFade(blackFadeImage, Color.black, 1f, 0f, duration);
    }

    public Coroutine FadeFromWhite(float duration)
    {
        return StartFade(whiteFadeImage, Color.white, 1f, 0f, duration);
    }

    public Coroutine FadeToWhite(float duration)
    {
        return StartFade(whiteFadeImage, Color.white, 0f, 1f, duration);
    }

    private static void SetImageAlpha(Image image, Color color, float alpha)
    {
        if (image == null)
            return;

        image.gameObject.SetActive(true);
        image.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }

    private Coroutine StartFade(Image image, Color color, float from, float to, float duration)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(Fade(image, color, from, to, duration));
        return _fadeRoutine;
    }

    private IEnumerator Fade(Image image, Color color, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        SetImageAlpha(image, color, from);
        var elapsed = 0f;
        duration = Mathf.Max(duration, 0.01f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetImageAlpha(image, color, Mathf.SmoothStep(from, to, elapsed / duration));
            yield return null;
        }

        SetImageAlpha(image, color, to);
        _fadeRoutine = null;
    }
}
