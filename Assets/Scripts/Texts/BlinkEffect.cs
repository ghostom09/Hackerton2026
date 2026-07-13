using System.Collections.Generic;
using UnityEngine;

[TextEffect("blink")]
public class BlinkEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Color32[] _colors = new Color32[4];

    public BlinkEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 8f);
        var minAlpha = _params.GetValueOrDefault("minAlpha", 0.05f);
        var t = Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.5f) * 0.5f + 0.5f;
        var alphaMul = Mathf.Lerp(minAlpha, 1f, t);

        for (var i = 0; i < 4; i++)
        {
            var c = charCtx.originalColors[i];
            _colors[i] = new Color32(c.r, c.g, c.b, (byte)(c.a * alphaMul));
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }
}
