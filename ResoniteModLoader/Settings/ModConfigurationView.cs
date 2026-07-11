using System.Globalization;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public sealed class ModConfigurationView : Component
{
    private ResoniteModBase? _mod;
    private ModConfiguration? _config;

    private readonly Dictionary<ModConfigurationKey, Checkbox>
        _boolInputs = new();

    private readonly Dictionary<ModConfigurationKey, TextField>
        _textInputs = new();

    public void Setup(ResoniteModBase mod)
    {
        _mod = mod;
        _config = mod.GetConfiguration();

        Build();
    }

    private void Build()
    {
        if (_mod == null)
            return;

        UIBuilder ui = RadiantUI_Panel.SetupPanel(
            Slot,
            "Mod Settings: " + _mod.Name,
            new float2(1000f, 1500f),
            pinButton: true);

        Slot.LocalScale *= 0.0005f;

        RadiantUI_Constants.SetupDefaultStyle(ui);

        ui.VerticalLayout(4f);

        ui.Style.MinHeight = 1050f;
        ui.Style.PreferredHeight = 1050f;

        ui.ScrollArea();

        ui.VerticalLayout(
            6f,
            8f,
            Alignment.TopLeft,
            true,
            false);

        ui.FitContent(
            SizeFit.Disabled,
            SizeFit.PreferredSize);

        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;

        ui.Text(
            "Mod: " +
            _mod.Name +
            " " +
            _mod.Version);

        ui.Text(
            "Author: " +
            _mod.Author);

        ui.Text(
            $"Settings: " +
            $"{_config?.ConfigurationItemDefinitions.Count ?? 0}");

        ui.Text("");

        if (_config == null)
        {
            ui.Text("This mod has no configuration.");
            return;
        }

        foreach (ModConfigurationKey key in
                 _config.ConfigurationItemDefinitions
                     .Where(k => !k.InternalAccessOnly)
                     .OrderBy(k => k.Name))
        {
            AddSetting(ui, key);
        }

        ui.NestOut();

        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;

        Button save =
            ui.Button(
                (LocaleString)"Save Settings");

        save.Pressed.Target =
            SaveSettings;
    }

    private void AddSetting(
        UIBuilder ui,
        ModConfigurationKey key)
    {
        if (_config == null)
            return;

        Type type =
            key.ValueType();

        object? value =
            GetValueOrDefault(key);

        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;

        LocaleString label =
            (LocaleString)key.Name;

        if (type == typeof(bool))
        {
            _boolInputs[key] =
                ui.HorizontalElementWithLabel(
                    label,
                    0.7f,
                    () => ui.Checkbox(
                        value is bool b && b));

            return;
        }

        _textInputs[key] =
            ui.HorizontalElementWithLabel(
                label,
                0.35f,
                () => ui.TextField(
                    value?.ToString() ?? ""));
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    public void SaveSettings(
        IButton button,
        ButtonEventData eventData)
    {
        if (_mod == null || _config == null)
            return;

        foreach (var pair in _boolInputs)
        {
            _config.Set(
                pair.Key,
                pair.Value.IsChecked);
        }

        foreach (var pair in _textInputs)
        {
            ModConfigurationKey key =
                pair.Key;

            TextField field =
                pair.Value;

            string text =
                field.TargetString ?? "";

            try
            {
                object parsed =
                    ParseValue(
                        key.ValueType(),
                        text);

                _config.Set(
                    key,
                    parsed);
            }
            catch (Exception ex)
            {
                UniLog.Warning(
                    $"Failed to parse '{key.Name}': {ex.Message}");
            }
        }

        _config.Save();

        Logger.MsgInternal(
            "Saved mod settings: " +
            _mod.Name);
    }

    private object? GetValueOrDefault(
        ModConfigurationKey key)
    {
        if (_config == null)
            return null;

        try
        {
            return _config.GetValue(key);
        }
        catch
        {
            key.TryComputeDefault(
                out object? value);

            return value;
        }
    }

    private static object ParseValue(
        Type type,
        string text)
    {
        return type switch
        {
            _ when type == typeof(string) =>
                text,

            _ when type == typeof(int) =>
                int.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type == typeof(float) =>
                float.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type == typeof(double) =>
                double.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type == typeof(long) =>
                long.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type == typeof(short) =>
                short.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type == typeof(byte) =>
                byte.Parse(
                    text,
                    CultureInfo.InvariantCulture),

            _ when type.IsEnum =>
                Enum.Parse(
                    type,
                    text,
                    true),

            _ => text
        };
    }
}