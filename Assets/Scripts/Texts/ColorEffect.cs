using System.Collections.Generic;
using UnityEngine;

[TextEffect("color")]
public class ColorEffect : ITextEffect
{
    private readonly byte _r;
    private readonly byte _g;
    private readonly byte _b;
    private readonly bool _hasAlpha;
    private readonly byte _a;
    private readonly Color32[] _colors = new Color32[4];

    public ColorEffect(Dictionary<string, float> parameters)
    {
        _r = ToByte(parameters.GetValueOrDefault("r", 255f));
        _g = ToByte(parameters.GetValueOrDefault("g", 255f));
        _b = ToByte(parameters.GetValueOrDefault("b", 255f));
        _hasAlpha = parameters.ContainsKey("a");
        _a = _hasAlpha ? ToByte(parameters["a"]) : (byte)255;
    }

    public void Apply(TMPRenderer renderer, TextEffectContext ctx)
    {
        if (!renderer.TryGetCharContext(ctx.charIndex, out var charCtx))
            return;

        for (var i = 0; i < 4; i++)
        {
            var alpha = _hasAlpha ? _a : charCtx.originalColors[i].a;
            _colors[i] = new Color32(_r, _g, _b, alpha);
        }

        renderer.SetCharacterColors(ctx.charIndex, _colors);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
    }
}
