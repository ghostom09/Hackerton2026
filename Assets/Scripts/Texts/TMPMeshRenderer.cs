using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TMPRenderer : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    private TMP_TextInfo _textInfo;
    private string _currentOutPutText;
    private readonly Dictionary<int, List<ITextEffect>> _charEffects = new Dictionary<int, List<ITextEffect>>();
    private readonly Dictionary<int, CharContext> _charContexts = new Dictionary<int, CharContext>();
    private float _textShownTime;
    private readonly HashSet<int> _dirtyMaterialIndices = new HashSet<int>();
    private readonly Dictionary<int, char> _displayCharOverrides = new Dictionary<int, char>();

    public TMP_Text TMPText => tmpText;
    public TMP_TextInfo TextInfo => _textInfo;
    public string CurrentText => _currentOutPutText;
    public float TextShownTime => _textShownTime;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += OnPreCanvasRender;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= OnPreCanvasRender;
    }

    private void EnsureInitialized()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        if (tmpText == null)
        {
            Debug.LogError("[TMPMeshRenderer] TMPText not found");
            return;
        }

        _textInfo = tmpText.textInfo;
    }

    private void OnPreCanvasRender()
    {
        if (tmpText is TextMeshProUGUI)
            ApplyEffects();
    }

    private void LateUpdate()
    {
        if (tmpText != null && tmpText is not TextMeshProUGUI)
            ApplyEffects();
    }

    private void ApplyEffects()
    {
        if (tmpText == null || _charEffects.Count == 0)
            return;

        _textInfo = tmpText.textInfo;
        if (_textInfo.characterCount == 0)
            return;

        if (_charContexts.Count == 0)
            CaptureCharContexts();

        _dirtyMaterialIndices.Clear();
        _displayCharOverrides.Clear();

        var time = Time.time;

        foreach (var pair in _charEffects)
        {
            var charIndex = pair.Key;
            if (charIndex < 0 || charIndex >= _currentOutPutText.Length)
                continue;

            if (!TryGetCharContext(charIndex, out _))
                continue;

            var ctx = new TextEffectContext
            {
                charIndex = charIndex,
                time = time
            };

            foreach (var effect in pair.Value)
            {
                if (effect is ScrambleEffect)
                    effect.Apply(this, ctx);
            }
        }

        ApplyDisplayTextOverrides();

        _textInfo = tmpText.textInfo;

        foreach (var pair in _charEffects)
        {
            var charIndex = pair.Key;
            if (charIndex < 0 || charIndex >= _textInfo.characterCount)
                continue;

            if (!TryGetCharContext(charIndex, out _))
                continue;

            if (!_textInfo.characterInfo[charIndex].isVisible)
                continue;

            if (_displayCharOverrides.ContainsKey(charIndex))
                continue;

            ResetCharacterMesh(charIndex);

            var ctx = new TextEffectContext
            {
                charIndex = charIndex,
                time = time
            };

            foreach (var effect in pair.Value)
            {
                if (effect is ScrambleEffect or TypingEffect)
                    continue;

                effect.Apply(this, ctx);
            }

            foreach (var effect in pair.Value)
            {
                if (effect is TypingEffect typing)
                    typing.Apply(this, ctx);
            }
        }

        FlushDirtyMaterials();
    }

    private void ApplyDisplayTextOverrides()
    {
        if (_displayCharOverrides.Count == 0)
        {
            if (tmpText.text != _currentOutPutText)
            {
                tmpText.text = _currentOutPutText;
                tmpText.ForceMeshUpdate();
            }

            return;
        }

        var chars = _currentOutPutText.ToCharArray();
        foreach (var pair in _displayCharOverrides)
        {
            if (pair.Key >= 0 && pair.Key < chars.Length)
                chars[pair.Key] = pair.Value;
        }

        var displayText = new string(chars);
        if (tmpText.text != displayText)
        {
            tmpText.text = displayText;
            tmpText.ForceMeshUpdate();
        }
    }

    private void FlushDirtyMaterials()
    {
        foreach (var matIndex in _dirtyMaterialIndices)
        {
            var meshInfo = _textInfo.meshInfo[matIndex];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.SetUVs(0, meshInfo.uvs0);
            meshInfo.mesh.colors32 = meshInfo.colors32;
            tmpText.UpdateGeometry(meshInfo.mesh, matIndex);
        }
    }

    public bool TryGetCharContext(int charIndex, out CharContext context)
    {
        return _charContexts.TryGetValue(charIndex, out context);
    }

    public void ResetCharacterMesh(int charIndex)
    {
        if (!TryGetCharContext(charIndex, out var ctx))
            return;

        if (!TryGetCharacterMeshIndices(charIndex, out var matIndex, out var vertIndex))
            return;

        var meshInfo = _textInfo.meshInfo[matIndex];
        var vertices = meshInfo.vertices;
        var uvs = meshInfo.uvs0;
        var colors = meshInfo.colors32;

        for (var v = 0; v < 4; v++)
        {
            vertices[vertIndex + v] = ctx.originalVertices[v];
            uvs[vertIndex + v] = ctx.originalUVs[v];
            colors[vertIndex + v] = ctx.originalColors[v];
        }

        meshInfo.vertices = vertices;
        meshInfo.uvs0 = uvs;
        meshInfo.colors32 = colors;
        _dirtyMaterialIndices.Add(matIndex);
    }

    public void SetCharacterVertices(int charIndex, Vector3[] vertices)
    {
        if (vertices == null || vertices.Length < 4)
            return;

        if (!TryGetCharacterMeshIndices(charIndex, out var matIndex, out var vertIndex))
            return;

        var meshInfo = _textInfo.meshInfo[matIndex];
        var meshVertices = meshInfo.vertices;

        for (var v = 0; v < 4; v++)
            meshVertices[vertIndex + v] = vertices[v];

        meshInfo.vertices = meshVertices;
        _dirtyMaterialIndices.Add(matIndex);
    }

    public void SetCharacterColors(int charIndex, Color32[] colors)
    {
        if (colors == null || colors.Length < 4)
            return;

        if (!TryGetCharacterMeshIndices(charIndex, out var matIndex, out var vertIndex))
            return;

        var meshInfo = _textInfo.meshInfo[matIndex];
        var meshColors = meshInfo.colors32;

        for (var v = 0; v < 4; v++)
            meshColors[vertIndex + v] = colors[v];

        meshInfo.colors32 = meshColors;
        _dirtyMaterialIndices.Add(matIndex);
    }

    public void SetDisplayCharacter(int charIndex, char displayChar)
    {
        _displayCharOverrides[charIndex] = displayChar;
    }

    public void SetAppearTime(int charIndex, float appearTime)
    {
        if (!_charContexts.TryGetValue(charIndex, out var ctx))
            return;

        ctx.appearTime = appearTime;
        _charContexts[charIndex] = ctx;
    }

    public bool TryGetCharRevealTime(int charIndex, out float revealTime)
    {
        revealTime = _charContexts.TryGetValue(charIndex, out var ctx) ? ctx.appearTime : _textShownTime;

        if (!_charEffects.TryGetValue(charIndex, out var effects))
            return false;

        foreach (var effect in effects)
        {
            if (effect is not TypingEffect typing)
                continue;

            revealTime = typing.StartTime + (charIndex - typing.RangeStart) * typing.CharDelay;
            return true;
        }

        return false;
    }

    public void ScheduleAppearTimes(int startIndex, int endIndex, float charDelay, float baseTime = -1f)
    {
        var startTime = baseTime < 0f ? Time.time : baseTime;

        for (var i = startIndex; i <= endIndex; i++)
            SetAppearTime(i, startTime + (i - startIndex) * charDelay);
    }

    public void AppendCharacter(char c)
    {
        EnsureInitialized();
        _currentOutPutText += c;
        UpdateText();
        RefreshMeshInfo();
        CaptureCharContextForIndex(_currentOutPutText.Length - 1);
    }

    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        EnsureInitialized();
        var startIndex = _currentOutPutText.Length;
        _currentOutPutText += text;
        UpdateText();
        RefreshMeshInfo();

        for (var i = 0; i < text.Length; i++)
            CaptureCharContextForIndex(startIndex + i);
    }

    public void AddText(string text)
    {
        AppendText(text);
    }

    private bool TryGetCharacterMeshIndices(int charIndex, out int matIndex, out int vertIndex)
    {
        matIndex = 0;
        vertIndex = 0;

        if (_textInfo == null || charIndex < 0 || charIndex >= _textInfo.characterCount)
            return false;

        var charInfo = _textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible)
            return false;

        matIndex = charInfo.materialReferenceIndex;
        vertIndex = charInfo.vertexIndex;
        return true;
    }

    private void UpdateText()
    {
        tmpText.text = _currentOutPutText;
    }

    public void Add(char c)
    {
        AppendCharacter(c);
    }

    public Vector3[] GetMesh(int index)
    {
        return _textInfo.meshInfo[index].vertices;
    }

    public void SetMesh(int index, Vector3[] vertices)
    {
        _textInfo.meshInfo[index].mesh.vertices = vertices;
        tmpText.UpdateGeometry(_textInfo.meshInfo[index].mesh, index);
    }

    public void SetText(string text)
    {
        EnsureInitialized();
        _currentOutPutText = text;
        UpdateText();
        RefreshMeshInfo();
        CaptureCharContexts();
    }

    public void RefreshCharContexts()
    {
        EnsureInitialized();
        RefreshMeshInfo();
        CaptureCharContexts();
    }

    public void ClearEffects()
    {
        _charEffects.Clear();
    }

    public void RegisterEffect(int charIndex, ITextEffect effect)
    {
        if (effect == null)
            return;

        if (!_charEffects.TryGetValue(charIndex, out var list))
        {
            list = new List<ITextEffect>();
            _charEffects[charIndex] = list;
        }

        list.Add(effect);
    }

    private void RefreshMeshInfo()
    {
        tmpText.ForceMeshUpdate();
        _textInfo = tmpText.textInfo;
        _textShownTime = Time.time;
    }

    private void CaptureCharContexts()
    {
        _charContexts.Clear();

        for (var i = 0; i < _currentOutPutText.Length && i < _textInfo.characterCount; i++)
            CaptureCharContextForIndex(i);
    }

    private void CaptureCharContextForIndex(int charIndex)
    {
        if (_textInfo == null || charIndex < 0 || charIndex >= _textInfo.characterCount)
            return;

        var charInfo = _textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible)
            return;

        var matIndex = charInfo.materialReferenceIndex;
        var vertIndex = charInfo.vertexIndex;
        var meshInfo = _textInfo.meshInfo[matIndex];

        var originalVertices = new Vector3[4];
        var originalUVs = new Vector4[4];
        var originalColors = new Color32[4];

        for (var v = 0; v < 4; v++)
        {
            originalVertices[v] = meshInfo.vertices[vertIndex + v];
            originalUVs[v] = meshInfo.uvs0[vertIndex + v];
            originalColors[v] = meshInfo.colors32[vertIndex + v];
        }

        _charContexts[charIndex] = new CharContext
        {
            charIndex = charIndex,
            appearTime = _textShownTime,
            originalCharacter = _currentOutPutText[charIndex],
            originalVertices = originalVertices,
            originalUVs = originalUVs,
            originalColors = originalColors
        };
    }
}
