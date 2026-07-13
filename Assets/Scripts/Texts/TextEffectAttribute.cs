using System;

[AttributeUsage(AttributeTargets.Class)]
public class TextEffectAttribute : Attribute
{
    public string CodeName { get; }

    public TextEffectAttribute(string codeName)
    {
        CodeName = codeName;
    }
}
