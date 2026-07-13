using System.Collections.Generic;
using UnityEngine;

[TextEffect("glitch")]
public class GlitchEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public GlitchEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var intensity = _params.GetValueOrDefault("intensity", 3f);
        var speed = _params.GetValueOrDefault("speed", 15f);
        var tick = Mathf.FloorToInt(ctx.time * speed) + ctx.charIndex * 17;
        var hash = Mathf.Abs(tick * 1103515245 + 12345);

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i];

        if (hash % 5 == 0)
        {
            var offset = new Vector3(
                (hash % 7 - 3) * intensity * 0.5f,
                ((hash / 7) % 5 - 2) * intensity * 0.5f,
                0f);

            for (var i = 0; i < 4; i++)
                _verts[i] += offset;
        }

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
