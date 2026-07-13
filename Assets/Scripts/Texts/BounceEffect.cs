using System.Collections.Generic;
using UnityEngine;

[TextEffect("bounce")]
public class BounceEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public BounceEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var amplitude = _params.GetValueOrDefault("amplitude", 8f);
        var frequency = _params.GetValueOrDefault("frequency", 6f);
        var bounce = Mathf.Abs(Mathf.Sin(ctx.time * frequency + ctx.charIndex * 0.4f)) * amplitude;

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + new Vector3(0f, bounce, 0f);

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
