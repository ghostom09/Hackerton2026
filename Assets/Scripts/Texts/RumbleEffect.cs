using System.Collections.Generic;
using UnityEngine;

[TextEffect("rumble")]
public class RumbleEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public RumbleEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var intensity = _params.GetValueOrDefault("intensity", 6f);
        var speed = _params.GetValueOrDefault("speed", 55f);
        var offsetX = Mathf.Sin(ctx.time * speed + ctx.charIndex * 1.7f) * intensity;
        var offsetY = Mathf.Cos(ctx.time * (speed * 0.83f) + ctx.charIndex * 2.3f) * intensity * 0.6f;
        var offset = new Vector3(offsetX, offsetY, 0f);

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + offset;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
