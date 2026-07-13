using System.Collections.Generic;
using UnityEngine;

[TextEffect("scramble")]
public class ScrambleEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;

    public ScrambleEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var respectAlpha = _params.GetValueOrDefault("respectAlpha", 0f) >= 1f;
        var revealTime = _params.GetValueOrDefault("revealTime", 1.5f);

        float scrambleStart;
        if (respectAlpha)
        {
            scrambleStart = charCtx.appearTime;
        }
        else if (renderer.TryGetCharRevealTime(ctx.charIndex, out var revealAt))
        {
            scrambleStart = revealAt;
        }
        else
        {
            scrambleStart = charCtx.appearTime;
        }

        if (!respectAlpha && ctx.time < scrambleStart)
            return;

        var elapsed = ctx.time - scrambleStart;

        if (elapsed >= revealTime)
            return;

        var tick = Mathf.FloorToInt(elapsed * 15f) + ctx.charIndex * 7919;
        const int minAscii = 33;
        const int maxAscii = 126;
        var asciiCode = minAscii + Mathf.Abs(tick) % (maxAscii - minAscii + 1);
        renderer.SetDisplayCharacter(ctx.charIndex, (char)asciiCode);
    }
}
