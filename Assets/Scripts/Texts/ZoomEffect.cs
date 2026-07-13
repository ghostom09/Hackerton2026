using System.Collections.Generic;
using UnityEngine;

[TextEffect("zoom")]
public class ZoomEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public ZoomEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var minScale = _params.GetValueOrDefault("minScale", 0.7f);
        var maxScale = _params.GetValueOrDefault("maxScale", 1.45f);
        var speed = _params.GetValueOrDefault("speed", 4f);
        var t = Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.4f) * 0.5f + 0.5f;
        var scale = Mathf.Lerp(minScale, maxScale, t);
        var center = (charCtx.originalVertices[0] + charCtx.originalVertices[1] +
                      charCtx.originalVertices[2] + charCtx.originalVertices[3]) * 0.25f;

        for (var i = 0; i < 4; i++)
            _verts[i] = center + (charCtx.originalVertices[i] - center) * scale;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
