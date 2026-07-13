using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TextReader : MonoBehaviour
{
    public class TagInfo
    {
        public string name;
        public Dictionary<string, float> attributes = new Dictionary<string, float>();
    }
    
    public class EffectRange
    {
        public string name;
        public int startIndex; 
        public int endIndex;   
        public Dictionary<string, float> attributes;
    }

    private readonly StringBuilder _effectText = new StringBuilder();
    private readonly StringBuilder _realText = new StringBuilder();
    private bool _isFlagClosed = true;
    private readonly Stack<(TagInfo tag, int startIndex)> _tagStack = new Stack<(TagInfo, int)>();
    public readonly List<EffectRange> effectRanges = new List<EffectRange>();
    public string OutputText => _realText.ToString();

    void Start()
    {
        foreach (var range in effectRanges)
        {
            print($"[{range.name}] {range.startIndex} ~ {range.endIndex}");
        }
    }

    public void Read(string str)
    {
        Reset();
        foreach (var c in str)
        {
            if (c == '<' && _isFlagClosed)
            {
                _isFlagClosed = false;
            }
            else if (c == '>' && !_isFlagClosed)
            {
                _isFlagClosed = true;
                var temp = _effectText.ToString();
                _effectText.Clear();
                temp = temp.Trim();

                if (temp.Length > 0 && temp[0] == '/')
                {
                    var closeName = temp.Split(' ')[0].Substring(1);

                    if (_tagStack.Count > 0)
                    {
                        var (openTag, startIndex) = _tagStack.Pop();

                        if (openTag.name == closeName)
                        {
                            effectRanges.Add(new EffectRange
                            {
                                name = openTag.name,
                                startIndex = startIndex,
                                endIndex = _realText.Length - 1,
                                attributes = openTag.attributes
                            });
                        }
                        else
                        {
                            print($"태그 불일치: 여는 태그 '{openTag.name}' / 닫는 태그 '{closeName}'");
                        }
                    }
                }
                else
                {
                    var tagInfo = ParseTag(temp);
                    _tagStack.Push((tagInfo, _realText.Length)); 
                }
            }
            else
            {
                if (!_isFlagClosed)
                {
                    _effectText.Append(c);
                }
                else
                {
                    _realText.Append(c);
                }
            }
        }
    }
    
    private TagInfo ParseTag(string raw)
    {
        var tagInfo = new TagInfo();
        var nameEnd = raw.IndexOf(' ');
        if (nameEnd < 0)
        {
            tagInfo.name = raw;
            return tagInfo;
        }

        tagInfo.name = raw.Substring(0, nameEnd);
        ParseAttributes(raw.Substring(nameEnd + 1), tagInfo.attributes);
        return tagInfo;
    }

    private static void ParseAttributes(string attrString, Dictionary<string, float> attributes)
    {
        var i = 0;
        while (i < attrString.Length)
        {
            var eqIndex = attrString.IndexOf('=', i);
            if (eqIndex < 0)
                break;

            var key = attrString.Substring(i, eqIndex - i).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[TextReader] Invalid attribute key near index {i} in '{attrString}'");
                break;
            }

            i = eqIndex + 1;
            while (i < attrString.Length && char.IsWhiteSpace(attrString[i]))
                i++;

            string valueStr;
            if (i < attrString.Length && attrString[i] == '"')
            {
                i++;
                var endQuote = attrString.IndexOf('"', i);
                if (endQuote < 0)
                {
                    Debug.LogError($"[TextReader] Unclosed quote in attribute '{key}'");
                    valueStr = attrString.Substring(i);
                    i = attrString.Length;
                }
                else
                {
                    valueStr = attrString.Substring(i, endQuote - i);
                    i = endQuote + 1;
                }
            }
            else
            {
                var valueEnd = i;
                while (valueEnd < attrString.Length && !char.IsWhiteSpace(attrString[valueEnd]))
                    valueEnd++;

                valueStr = attrString.Substring(i, valueEnd - i).Trim();
                i = valueEnd;
            }

            if (!float.TryParse(valueStr, out var value))
            {
                Debug.LogError($"[TextReader] Failed to parse attribute '{key}' = '{valueStr}' as number.");
            }
            else
            {
                attributes[key] = value;
            }

            while (i < attrString.Length && char.IsWhiteSpace(attrString[i]))
                i++;
        }
    }

    private void Reset()
    {
        _effectText.Clear();
        _realText.Clear();
        _isFlagClosed = true;
        _tagStack.Clear();
        effectRanges.Clear();
    }
}
