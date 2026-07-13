using System.Collections.Generic;
using UnityEngine;

[TextEffect("rainbow")]
public class RainbowEffect : ITextEffect
{
    private readonly Dictionary<string, float> _params;
    private readonly Color32[] _colors = new Color32[4];

    public RainbowEffect(Dictionary<string, float> parameters)
    {
        _params = parameters;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        var speed = _params.GetValueOrDefault("speed", 1f);
        var saturation = _params.GetValueOrDefault("saturation", 1f);
        var spread = _params.GetValueOrDefault("spread", 0.1f);

        var hue = Mathf.Repeat(ctx.time * speed + ctx.charIndex * spread, 1f);
        var rainbow = Color.HSVToRGB(hue, saturation, 1f);

        for (var i = 0; i < 4; i++)
        {
            var c = charCtx.originalColors[i];
            _colors[i] = new Color32(
                (byte)(rainbow.r * 255f),
                (byte)(rainbow.g * 255f),
                (byte)(rainbow.b * 255f),
                c.a);
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }
}
