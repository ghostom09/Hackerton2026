using System.Collections.Generic;
using UnityEngine;

[TextEffect("jitter")]
public class JitterEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public JitterEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var intensity = _params.GetValueOrDefault("intensity", 4f);
        var speed = _params.GetValueOrDefault("speed", 40f);
        var seed = Mathf.FloorToInt(ctx.time * speed) + ctx.charIndex * 97;
        var hash = Mathf.Abs(seed * 1103515245 + 12345);
        var offset = new Vector3(
            ((hash % 100) / 50f - 1f) * intensity,
            (((hash / 100) % 100) / 50f - 1f) * intensity,
            0f);

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + offset;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
