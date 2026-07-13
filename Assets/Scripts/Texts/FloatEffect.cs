using System.Collections.Generic;
using UnityEngine;

[TextEffect("float")]
public class FloatEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public FloatEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var amplitude = _params.GetValueOrDefault("amplitude", 5f);
        var speed = _params.GetValueOrDefault("speed", 2f);
        var offsetY = Mathf.Sin(ctx.time * speed + ctx.charIndex * 0.6f) * amplitude;

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + new Vector3(0f, offsetY, 0f);

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
