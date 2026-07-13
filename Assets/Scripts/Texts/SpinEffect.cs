using System.Collections.Generic;
using UnityEngine;

[TextEffect("spin")]
public class SpinEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public SpinEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 180f);
        var rad = (ctx.time * speed + ctx.charIndex * 25f) * Mathf.Deg2Rad;
        var center = (charCtx.originalVertices[0] + charCtx.originalVertices[1] +
                      charCtx.originalVertices[2] + charCtx.originalVertices[3]) * 0.25f;
        var cos = Mathf.Cos(rad);
        var sin = Mathf.Sin(rad);

        for (var i = 0; i < 4; i++)
        {
            var local = charCtx.originalVertices[i] - center;
            _verts[i] = center + new Vector3(local.x * cos - local.y * sin, local.x * sin + local.y * cos, 0f);
        }

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
