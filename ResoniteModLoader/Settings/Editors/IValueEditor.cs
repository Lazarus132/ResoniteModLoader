using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public interface IValueEditor
{
    string Prefix { get; }

    string SerializedValue { get; }

    void SetSerializedValue(
        string value);

    Component Build(
        UIBuilder ui);
}

/// <summary>
/// Optional interface for editors which contain their own buttons.
/// ModConfigurationView routes the UIX button events back to the
/// corresponding editor instance.
/// </summary>
public interface IButtonValueEditor
{
    IEnumerable<Button> EditorButtons { get; }

    void HandleButton(
        IButton button);
}