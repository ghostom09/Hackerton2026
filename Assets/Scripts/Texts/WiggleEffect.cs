using System.Collections.Generic;
using UnityEngine;

[TextEffect("wiggle")]
public class WiggleEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public WiggleEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 8f);
        var angle = _params.GetValueOrDefault("angle", 12f);
        var rotation = Mathf.Sin(ctx.time * speed + ctx.charIndex) * angle;
        RotateVertices(charCtx.originalVertices, _verts, rotation);

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }

    private static void RotateVertices(Vector3[] source, Vector3[] destination, float degrees)
    {
        var center = (source[0] + source[1] + source[2] + source[3]) * 0.25f;
        var rad = degrees * Mathf.Deg2Rad;
        var cos = Mathf.Cos(rad);
        var sin = Mathf.Sin(rad);

        for (var i = 0; i < 4; i++)
        {
            var offset = source[i] - center;
            destination[i] = center + new Vector3(
                offset.x * cos - offset.y * sin,
                offset.x * sin + offset.y * cos,
                offset.z);
        }
    }
}
