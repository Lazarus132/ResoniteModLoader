using System;
using System.Collections.Generic;
using Elements.Core;

namespace ResoniteModLoader;

public static class EditorRegistry
{
    private static readonly Dictionary<
        Type,
        Func<IValueEditor>> _editorFactories =
            new();

    static EditorRegistry()
    {
        Register(
            typeof(colorX),
            static () =>
                new ColorEditor());
    }

    public static void Register(
        Type valueType,
        Func<IValueEditor> factory)
    {
        ArgumentNullException.ThrowIfNull(
            valueType);

        ArgumentNullException.ThrowIfNull(
            factory);

        _editorFactories[valueType] =
            factory;
    }

    public static IValueEditor GetEditor(
        Type valueType)
    {
        if (_editorFactories.TryGetValue(
                valueType,
                out Func<IValueEditor>? factory))
        {
            return factory();
        }

        return new TextEditor();
    }

    private static string? ExtractPrefix(
        string serializedValue)
    {
        if (string.IsNullOrEmpty(
                serializedValue))
        {
            return null;
        }

        if (!serializedValue.StartsWith(
                "[",
                StringComparison.Ordinal))
        {
            return null;
        }

        int closingBracket =
            serializedValue.IndexOf(']');

        if (closingBracket <= 1)
            return null;

        return serializedValue.Substring(
            1,
            closingBracket - 1);
    }
}