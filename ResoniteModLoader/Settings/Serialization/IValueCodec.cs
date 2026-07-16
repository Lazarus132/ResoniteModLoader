using System;

namespace ResoniteModLoader;

public interface IValueCodec
{
    Type ValueType { get; }

    string Prefix { get; }

    string Serialize(
        object value);

    object Deserialize(
        string text);
}