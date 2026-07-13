using System.Collections.Generic;
using UnityEngine;

[TextEffect("type")]
public class TypingEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Color32[] _colors = new Color32[4];

    public int RangeStart { get; set; }
    public float StartTime { get; set; }

    public float CharDelay => _params.GetValueOrDefault("charDelay", 0.05f);

    public TypingEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var indexInRange = ctx.charIndex - RangeStart;
        var showAt = StartTime + indexInRange * CharDelay;

        if (ctx.time >= showAt)
            return;

        for (var i = 0; i < 4; i++)
        {
            var c = charCtx.originalColors[i];
            _colors[i] = new Color32(c.r, c.g, c.b, 0);
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }
}
