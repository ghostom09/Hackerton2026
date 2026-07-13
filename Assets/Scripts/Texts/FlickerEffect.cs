using System.Collections.Generic;
using UnityEngine;

[TextEffect("flicker")]
public class FlickerEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Color32[] _colors = new Color32[4];

    public FlickerEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 20f);
        var minAlpha = _params.GetValueOrDefault("minAlpha", 0.3f);
        var alpha = minAlpha + (1f - minAlpha) * (0.5f + 0.5f * Mathf.Sin(ctx.time * speed + ctx.charIndex));

        for (var i = 0; i < 4; i++)
        {
            var c = charCtx.originalColors[i];
            _colors[i] = new Color32(c.r, c.g, c.b, (byte)(c.a * alpha));
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }
}
