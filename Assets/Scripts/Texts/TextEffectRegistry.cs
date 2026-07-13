using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class TextEffectRegistry
{
    private static Dictionary<string, Func<Dictionary<string, float>, ITextEffect>> _effects;

    public static IReadOnlyDictionary<string, Func<Dictionary<string, float>, ITextEffect>> Effects
    {
        get
        {
            if (_effects == null)
                Build();
            return _effects;
        }
    }

    private static void Build()
    {
        _effects = new Dictionary<string, Func<Dictionary<string, float>, ITextEffect>>();

        foreach (var type in FindEffectTypes())
        {
            var attribute = type.GetCustomAttribute<TextEffectAttribute>();
            if (attribute == null)
                continue;

            var ctor = type.GetConstructor(new[] { typeof(Dictionary<string, float>) });
            if (ctor == null)
            {
                Debug.LogWarning($"[TextEffectRegistry] {type.Name} needs a constructor(Dictionary<string, float>).");
                continue;
            }

            if (_effects.ContainsKey(attribute.CodeName))
            {
                Debug.LogWarning($"[TextEffectRegistry] Duplicate code name '{attribute.CodeName}': {type.Name}");
                continue;
            }

            _effects[attribute.CodeName] = parameters => (ITextEffect)Activator.CreateInstance(type, parameters);
        }
    }

    private static IEnumerable<Type> FindEffectTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            if (types == null)
                continue;

            foreach (var type in types)
            {
                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                if (typeof(ITextEffect).IsAssignableFrom(type))
                    yield return type;
            }
        }
    }
}
