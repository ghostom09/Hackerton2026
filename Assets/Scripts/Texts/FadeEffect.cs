using System.Collections.Generic;
using UnityEngine;

[TextEffect("fade")]
public class FadeEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Color32[] _colors = new Color32[4];

    public FadeEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var fadeInDuration = _params.GetValueOrDefault("fadeInDuration", 0.2f);
        var elapsed = ctx.time - charCtx.appearTime;
        var alpha = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInDuration);

        for (var i = 0; i < 4; i++)
        {
            var c = charCtx.originalColors[i];
            _colors[i] = new Color32(c.r, c.g, c.b, (byte)(c.a * alpha));
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }
}
