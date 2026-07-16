using System.Globalization;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public sealed class ModConfigurationView : Component
{
    private ResoniteModBase? _mod;
    private ModConfiguration? _config;
    private const float FooterHeight = 64f;
    private const float SaveButtonTextSize  = 24f;
    private const float ContentPadding = 24f;
    private const float SettingHeight = 32f;
    private const float InnerPadding = 8f;
    private const float SettingSpacing = 6f;
    private readonly Dictionary<ModConfigurationKey, Checkbox> _boolInputs = new();
    private readonly Dictionary<ModConfigurationKey, IValueEditor> _editors = new();
    private readonly Dictionary<IButton, IButtonValueEditor> _editorButtonOwners = new();

    private static void ApplySettingHeight(UIBuilder ui)
    {
        ui.Style.MinHeight = SettingHeight;
        ui.Style.PreferredHeight = SettingHeight;
        ui.Style.FlexibleHeight = -1f;
    }

    public void Setup(ResoniteModBase mod)
    {
        _mod = mod;

        _config = mod.GetConfiguration();

        Build();
    }

    private void Build()
    {
        if (_mod == null) return;

        Slot.DestroyChildren();
        _boolInputs.Clear();
        _editors.Clear();
        _editorButtonOwners.Clear();

        UIBuilder ui = new UIBuilder(Slot);

        RadiantUI_Constants.SetupDefaultStyle(ui);

        /*
        * WICHTIG:
        *
        * Hier KEIN äußeres VerticalLayout erzeugen.
        *
        * HorizontalFooter arbeitet direkt mit RectTransform-Ankern
        * und teilt den gesamten verfügbaren Bereich sauber in:
        *
        * ├─ Content
        * └─ Footer
        */
        ui.HorizontalFooter(FooterHeight,
            out RectTransform footer,
            out RectTransform content);

        /*
        * ==========================================================
        * SETTINGS-CONTENT
        * ==========================================================
        */

        ui.NestInto(content);

        content.AddFixedPadding(ContentPadding);

        ui.ScrollArea(Alignment.TopLeft);

        ui.VerticalLayout(
            spacing: SettingSpacing,
            padding: InnerPadding,
            childAlignment: Alignment.TopLeft,
            forceExpandWidth: true,
            forceExpandHeight: false);

        ui.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
        ApplySettingHeight(ui);

        if (_config == null)
        {
            ui.Text("This mod has no configuration.");
            return;
        }

        foreach (ModConfigurationKey key in
            _config.ConfigurationItemDefinitions
                .Where(key => !key.InternalAccessOnly)
                .OrderBy(key => key.Name))
        {
            AddSetting(ui, key);
        }

        /*
        * ==========================================================
        * FOOTER
        * ==========================================================
        */

        footer.AddFixedPadding(InnerPadding);

        ui.ForceNext = footer;

        RectTransform footerPanel = ui.Panel();

        footerPanel.AddProportionalPadding(
            left: 0.25f,
            top: 0f,
            right: 0.25f,
            bottom: 0f);

        ui.Style.MinHeight = -1f;
        ui.Style.PreferredHeight = -1f;
        ui.Style.FlexibleHeight = -1f;

        Button save = ui.Button((LocaleString)"Save Settings");

        save.Label.Size.Value = SaveButtonTextSize;
        save.Pressed.Target = SaveSettings;

        ui.NestOut();
    }

    private void AddSetting(
        UIBuilder ui,
        ModConfigurationKey key)
    {
        if (_config == null) return;

        Type type = key.ValueType();

        object? value = GetValueOrDefault(key);

        ApplySettingHeight(ui);

        LocaleString label = (LocaleString)key.Name;

        if (type == typeof(bool))
        {
            _boolInputs[key] =
                ui.HorizontalElementWithLabel(
                    label, 0.7f, () => ui.Checkbox(
                        value is bool boolValue && boolValue));
            return;
        }

        IValueEditor editor = EditorRegistry.GetEditor(type);

        string serializedValue = ValueSerializer.Serialize(value);

        editor.SetSerializedValue(serializedValue);

        ui.HorizontalElementWithLabel(label, 0.35f, () => editor.Build(ui));

        _editors[key] = editor;

        if (editor is
            IButtonValueEditor buttonEditor)
        {
            foreach (Button editorButton in buttonEditor.EditorButtons)
            {
                editorButton.Pressed.Target = HandleEditorButton;
                _editorButtonOwners[editorButton] = buttonEditor;
            }
        }
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    public void HandleEditorButton(
        IButton button,
        ButtonEventData eventData)
    {
        if (_editorButtonOwners.TryGetValue(button, out IButtonValueEditor? editor))
        {
            editor.HandleButton(button);
        }
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    public void SaveSettings(
        IButton button,
        ButtonEventData eventData)
    {
        if (_mod == null || _config == null || 
            (_editors.Count == 0 && 
                _boolInputs.Count == 0)) return;

        foreach (var pair in _boolInputs)
        {
            _config.Set(pair.Key, pair.Value.IsChecked);
        }

        foreach (var pair in _editors)
        {
            ModConfigurationKey key = pair.Key;

            IValueEditor editor = pair.Value;

            string serializedValue = editor.SerializedValue;

            try
            {
                object parsedValue =
                    ParseValue(
                        key.ValueType(),
                        serializedValue);

                _config.Set(key, parsedValue);
            }
            catch (Exception exception)
            {
                UniLog.Warning(
                    $"Failed to parse " +
                    $"'{key.Name}': " +
                    exception.Message);
            }
        }
        try
        {
            _config.Save();
        }
        catch (Exception ex)
        {
            UniLog.Error($"Failed to save configuration: {ex}");
        }
    }

    private object? GetValueOrDefault(
        ModConfigurationKey key)
    {
        if (_config == null) return null;

        try
        {
            return _config.GetValue(key);
        }
        catch
        {
            key.TryComputeDefault(out object? value);
            return value;
        }
    }

    private static object ParseValue(
        Type type,
        string text)
    {
        return type switch
        {
            _ when type == typeof(colorX) => ValueSerializer.Deserialize(type, text),
            _ when type == typeof(string) => text,
            _ when type == typeof(int) => int.Parse(text, CultureInfo.InvariantCulture),
            _ when type == typeof(float) => float.Parse(text, CultureInfo.InvariantCulture),
            _ when type == typeof(double) => double.Parse(text, CultureInfo.InvariantCulture),
            _ when type == typeof(long) => long.Parse(text, CultureInfo.InvariantCulture),
            _ when type == typeof(short) => short.Parse(text, CultureInfo.InvariantCulture),
            _ when type == typeof(byte) => byte.Parse(text, CultureInfo.InvariantCulture),
            _ when type.IsEnum => Enum.Parse(type, text, true),
            _ => ValueSerializer.Deserialize(type, text)
        };
    }
}