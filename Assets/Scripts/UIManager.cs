using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum charEmotion
{
    happy,
    normal,
    sad,
    mad,
    menhara,
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private Image charProfile;
    [SerializeField] private float completeEmotionDuration = 1f;
    [SerializeField, Range(0f, 0.1f)] private float expressionFadeDuration = 0.08f;
    [SerializeField, Range(1f, 1.1f)] private float expressionScale = 1.02f;
    
    [System.Serializable]
    public struct ImageSet
    {
        public charEmotion emotion;
        public Sprite image;
    }
    public List<ImageSet> charImage;

    private Coroutine _emotionRoutine;
    private Coroutine _expressionRoutine;
    private Vector3 _profileBaseScale;

    public float CompleteEmotionDuration => completeEmotionDuration;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        if (charProfile != null)
            _profileBaseScale = charProfile.rectTransform.localScale;
    }

    public void CompleteMap()
    {
        ShowTemporaryEmotion(charEmotion.happy);
    }

    public void ShowSadForTimerReduced()
    {
        ShowTemporaryEmotion(charEmotion.sad);
    }

    public void ShowTemporaryEmotion(charEmotion emotion)
    {
        ShowEmotion(emotion);

        if (_emotionRoutine != null)
            StopCoroutine(_emotionRoutine);

        _emotionRoutine = StartCoroutine(ResetEmotionAfterDelay());
    }

    private IEnumerator ResetEmotionAfterDelay()
    {
        yield return new WaitForSeconds(completeEmotionDuration);
        ShowEmotion(charEmotion.normal);
        _emotionRoutine = null;
    }

    public void ShowEmotion(charEmotion emotion)
    {
        if (charProfile == null)
            return;

        if (_expressionRoutine != null)
            StopCoroutine(_expressionRoutine);

        RestoreProfilePresentation();
        _expressionRoutine = StartCoroutine(PlayExpressionChange(emotion));
    }

    private IEnumerator PlayExpressionChange(charEmotion emotion)
    {
        var nextSprite = GetEmotionSprite(emotion);
        if (nextSprite == null)
        {
            _expressionRoutine = null;
            yield break;
        }

        var color = charProfile.color;
        var fadeTime = Mathf.Max(expressionFadeDuration, 0.001f);

        // A short fade and 2% scale pulse make a static portrait feel responsive
        // without imitating a full Live2D animation.
        for (var elapsed = 0f; elapsed < fadeTime; elapsed += Time.unscaledDeltaTime)
        {
            var progress = elapsed / fadeTime;
            color.a = Mathf.Lerp(1f, 0.82f, progress);
            charProfile.color = color;
            charProfile.rectTransform.localScale = Vector3.Lerp(_profileBaseScale, _profileBaseScale * expressionScale, progress);
            yield return null;
        }

        charProfile.sprite = nextSprite;

        for (var elapsed = 0f; elapsed < fadeTime; elapsed += Time.unscaledDeltaTime)
        {
            var progress = elapsed / fadeTime;
            color.a = Mathf.Lerp(0.82f, 1f, progress);
            charProfile.color = color;
            charProfile.rectTransform.localScale = Vector3.Lerp(_profileBaseScale * expressionScale, _profileBaseScale, progress);
            yield return null;
        }

        color.a = 1f;
        charProfile.color = color;
        charProfile.rectTransform.localScale = _profileBaseScale;
        _expressionRoutine = null;
    }

    private void RestoreProfilePresentation()
    {
        var color = charProfile.color;
        color.a = 1f;
        charProfile.color = color;
        charProfile.rectTransform.localScale = _profileBaseScale;
    }

    private Sprite GetEmotionSprite(charEmotion emotion)
    {
        if (charImage == null)
            return null;

        foreach (var set in charImage)
        {
            if (set.emotion == emotion)
                return set.image;
        }

        return null;
    }
}
