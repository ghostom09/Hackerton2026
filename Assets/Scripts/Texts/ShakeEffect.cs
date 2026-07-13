using System.Collections.Generic;
using UnityEngine;

[TextEffect("shake")]
public class ShakeEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public ShakeEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var intensity = _params.GetValueOrDefault("intensity", 1f);
        var offsetX = Mathf.Sin(ctx.time * 50f + ctx.charIndex) * intensity;
        var offsetY = Mathf.Cos(ctx.time * 43f + ctx.charIndex) * intensity;
        var offset = new Vector3(offsetX, offsetY, 0f);

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + offset;

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
