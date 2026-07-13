using System.Collections.Generic;
using UnityEngine;

[TextEffect("jump")]
public class JumpEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Vector3[] _verts = new Vector3[4];

    public JumpEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var height = _params.GetValueOrDefault("height", 18f);
        var speed = _params.GetValueOrDefault("speed", 5f);
        var stagger = _params.GetValueOrDefault("stagger", 0.35f);
        var phase = ctx.time * speed - ctx.charIndex * stagger;
        var cycle = Mathf.Repeat(phase, 1f);
        // 포물선 점프
        var jump = cycle < 0.55f ? Mathf.Sin(cycle / 0.55f * Mathf.PI) * height : 0f;

        for (var i = 0; i < 4; i++)
            _verts[i] = charCtx.originalVertices[i] + new Vector3(0f, jump, 0f);

        renderer.SetCharacterVertices(ctx.charIndex, _verts);
    }
}
