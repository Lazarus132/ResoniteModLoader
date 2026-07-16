using System;
using System.Collections.Generic;

namespace ResoniteModLoader;

public static class ValueSerializer
{
    private static readonly Dictionary<
        Type,
        IValueCodec> _typeCodecs =
            new();

    static ValueSerializer()
    {
        Register(
            new ColorCodec());
    }

    public static void Register(
        IValueCodec codec)
    {
        _typeCodecs[codec.ValueType] =
            codec;
    }

    public static string Serialize(
        object? value)
    {
        if (value == null)
            return string.Empty;

        Type valueType =
            value.GetType();

        if (_typeCodecs.TryGetValue(
                valueType,
                out IValueCodec? codec))
        {
            return codec.Serialize(
                value);
        }

        return value.ToString()
            ?? string.Empty;
    }

    public static object Deserialize(
        Type targetType,
        string text)
    {
        if (_typeCodecs.TryGetValue(
                targetType,
                out IValueCodec? codec))
        {
            return codec.Deserialize(
                text);
        }

        throw new NotSupportedException(
            $"No value codec is registered for " +
            $"'{targetType.FullName}'.");
    }
}