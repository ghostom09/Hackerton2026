using System.Collections.Generic;
using UnityEngine;

[TextEffect("pulse")]
public class PulseEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public PulseEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 3f);
        var intensity = _params.GetValueOrDefault("intensity", 0.15f);
        var scale = 1f + Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.3f) * intensity;
        var center = GetCenter(charCtx.originalVertices);

        for (var i = 0; i < 4; i++)
            _verts[i] = center + (charCtx.originalVertices[i] - center) * scale;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }

    private static Vector3 GetCenter(Vector3[] vertices)
    {
        return (vertices[0] + vertices[1] + vertices[2] + vertices[3]) * 0.25f;
    }
}
