using System.Collections.Generic;
using UnityEngine;

[TextEffect("drip")]
public class DripEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public DripEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var amplitude = _params.GetValueOrDefault("amplitude", 14f);
        var speed = _params.GetValueOrDefault("speed", 3.5f);
        var stretch = _params.GetValueOrDefault("stretch", 0.35f);
        var wave = (Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.7f) * 0.5f + 0.5f);
        var drop = wave * amplitude;
        var center = (charCtx.originalVertices[0] + charCtx.originalVertices[1] +
                      charCtx.originalVertices[2] + charCtx.originalVertices[3]) * 0.25f;

        for (var i = 0; i < 4; i++)
        {
            var local = charCtx.originalVertices[i] - center;
            // 아래쪽 버텍스(0,3)를 더 늘려 녹아내리는 느낌
            var yScale = i == 0 || i == 3 ? 1f + wave * stretch : 1f - wave * stretch * 0.35f;
            _verts[i] = center + new Vector3(local.x, local.y * yScale - drop, 0f);
        }

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
