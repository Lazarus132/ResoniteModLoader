using System;
using System.Globalization;
using Elements.Core;

namespace ResoniteModLoader;

public sealed class ColorCodec
    : IValueCodec
{
    public Type ValueType =>
        typeof(colorX);

    public string Prefix =>
        "color";

    public string Serialize(
        object value)
    {
        colorX color =
            (colorX)value;

        byte r =
            FloatToByte(color.r);

        byte g =
            FloatToByte(color.g);

        byte b =
            FloatToByte(color.b);

        byte a =
            FloatToByte(color.a);

        return
            $"[{Prefix}]#" +
            $"{r:X2}" +
            $"{g:X2}" +
            $"{b:X2}" +
            $"{a:X2}";
    }

    public object Deserialize(
        string text)
    {
        string expectedPrefix =
            $"[{Prefix}]#";

        if (!text.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                $"Expected {expectedPrefix}RRGGBBAA");
        }

        string hex =
            text.Substring(
                expectedPrefix.Length);

        if (hex.Length != 8)
        {
            throw new FormatException(
                "Expected exactly 8 hexadecimal digits.");
        }

        byte r =
            ParseByte(hex, 0);

        byte g =
            ParseByte(hex, 2);

        byte b =
            ParseByte(hex, 4);

        byte a =
            ParseByte(hex, 6);

        return new colorX(
            r / 255f,
            g / 255f,
            b / 255f,
            a / 255f);
    }

    private static byte FloatToByte(
        float value)
    {
        float clamped =
            Math.Clamp(
                value,
                0f,
                1f);

        return (byte)MathF.Round(
            clamped * 255f);
    }

    private static byte ParseByte(
        string hex,
        int startIndex)
    {
        return byte.Parse(
            hex.Substring(
                startIndex,
                2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);
    }
}