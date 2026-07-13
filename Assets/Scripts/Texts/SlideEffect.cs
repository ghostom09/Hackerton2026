using System.Collections.Generic;
using UnityEngine;

[TextEffect("slide")]
public class SlideEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public SlideEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var duration = _params.GetValueOrDefault("duration", 0.3f);
        var distance = _params.GetValueOrDefault("distance", 40f);
        var elapsed = ctx.time - charCtx.appearTime;
        var t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        t = 1f - (1f - t) * (1f - t);

        var offset = new Vector3(Mathf.Lerp(-distance, 0f, t), 0f, 0f);

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + offset;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
