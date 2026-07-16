using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public sealed class TextEditor
    : IValueEditor
{
    private string _serializedValue =
        string.Empty;

    private TextField? _field;

    public string Prefix =>
        string.Empty;

    public string SerializedValue =>
        _field?.TargetString
        ?? _serializedValue;

    public void SetSerializedValue(
        string value)
    {
        _serializedValue =
            value ?? string.Empty;

        if (_field != null)
        {
            _field.TargetString =
                _serializedValue;
        }
    }

    public Component Build(
        UIBuilder ui)
    {
        _field =
            ui.TextField(
                _serializedValue);

        return _field;
    }
}