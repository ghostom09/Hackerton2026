using System.Collections.Generic;
using UnityEngine;

[TextEffect("wave")]
public class WaveEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public WaveEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var amplitude = _params.GetValueOrDefault("amplitude", 10f);
        var frequency = _params.GetValueOrDefault("frequency", 10f);
        var wave = Mathf.Sin(ctx.time * frequency + ctx.charIndex) * amplitude;

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + new Vector3(0f, wave, 0f);

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
