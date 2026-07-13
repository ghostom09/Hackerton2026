using System.Collections.Generic;
using UnityEngine;

[TextEffect("swing")]
public class SwingEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public SwingEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var angle = _params.GetValueOrDefault("angle", 20f);
        var speed = _params.GetValueOrDefault("speed", 5f);
        var rad = Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.45f) * angle * Mathf.Deg2Rad;
        var pivot = (charCtx.originalVertices[0] + charCtx.originalVertices[3]) * 0.5f;

        var cos = Mathf.Cos(rad);
        var sin = Mathf.Sin(rad);

        for (var i = 0; i < 4; i++)
        {
            var local = charCtx.originalVertices[i] - pivot;
            _verts[i] = pivot + new Vector3(local.x * cos - local.y * sin, local.x * sin + local.y * cos, 0f);
        }

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
