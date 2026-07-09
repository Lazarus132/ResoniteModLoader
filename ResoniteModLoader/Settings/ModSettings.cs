using System.Globalization;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

[AutoRegisterSetting]
[SettingCategory("ResoniteModLoader")]
public sealed class ModSettings : SettingComponent<ModSettings>
{
    public override bool UserspaceOnly => true;

#pragma warning disable CS8618
#pragma warning disable CA1051

    [SettingIndicatorProperty]
    public readonly RawOutput<string> SelectedMod;

    [SettingIndicatorProperty]
    public readonly RawOutput<string> ModCount;

    [SettingProperty]
    public readonly Sync<int> SelectedIndex;

#pragma warning restore CS8618, CA1051

    [SettingProperty("Previous Mod", "")]
    [SyncMethod(typeof(Action), [])]
    public void PreviousMod()
    {
        int count = GetMods().Count;

        if (count <= 0)
            return;

        SelectedIndex.Value--;

        if (SelectedIndex.Value < 0)
            SelectedIndex.Value = count - 1;

        UpdateSelectedMod();
    }

    [SettingProperty("Next Mod", "")]
    [SyncMethod(typeof(Action), [])]
    public void NextMod()
    {
        int count = GetMods().Count;

        if (count <= 0)
            return;

        SelectedIndex.Value++;

        if (SelectedIndex.Value >= count)
            SelectedIndex.Value = 0;

        UpdateSelectedMod();
    }

    [SettingProperty("Open Mod Settings", "")]
	[SyncMethod(typeof(Action), [])]
	public void OpenSelectedModSettings()
	{
		List<ResoniteModBase> mods = GetMods();

		if (mods.Count == 0)
			return;

		SelectedIndex.Value =
			MathX.Clamp(SelectedIndex.Value, 0, mods.Count - 1);

		ResoniteModBase mod = mods[SelectedIndex.Value];

		World world = Userspace.UserspaceWorld;

		if (world == null)
			return;

		Slot slot = world.AddSlot(
			"Mod Settings - " + mod.Name,
			false);

		slot.PositionInFrontOfUser(new float3?(float3.Backward));

		ModConfigurationView view =
			slot.AttachComponent<ModConfigurationView>();

		view.Setup(mod);
	}

    protected override void OnStart()
    {
        base.OnStart();
        SelectedIndex.Value = 0;
        UpdateSelectedMod();
    }

    protected override void OnChanges()
    {
        base.OnChanges();
        UpdateSelectedMod();
    }

    public override void ResetToDefault()
    {
        SelectedIndex.Value = 0;
        UpdateSelectedMod();
    }

    private void UpdateSelectedMod()
    {
        List<ResoniteModBase> mods = GetMods();

        ModCount.Value =
            mods.Count.ToString(CultureInfo.InvariantCulture);

        if (mods.Count == 0)
        {
            SelectedMod.Value = "No mods loaded";
            return;
        }

        if (SelectedIndex.Value < 0)
            SelectedIndex.Value = 0;

        if (SelectedIndex.Value >= mods.Count)
            SelectedIndex.Value = mods.Count - 1;

        ResoniteModBase mod = mods[SelectedIndex.Value];

        SelectedMod.Value =
            mod.Name + " " + mod.Version;
    }

    private static List<ResoniteModBase> GetMods()
    {
        return ModLoader.Mods()
            .OrderBy(m => m.Name)
            .Cast<ResoniteModBase>()
            .ToList();
    }
}

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

		ui.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);

		ui.Style.MinHeight = 32f;
		ui.Style.PreferredHeight = 32f;

		ui.Text("Mod: " + _mod.Name + " " + _mod.Version);
		ui.Text("Author: " + _mod.Author);
		ui.Text($"Settings: {_config?.ConfigurationItemDefinitions.Count ?? 0}");
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

		ui.Style.MinHeight = 50f;
		ui.Style.PreferredHeight = 50f;

		Button save = ui.Button((LocaleString)"Save Settings");
		save.Pressed.Target = SaveSettings;

		Button close = ui.Button((LocaleString)"Close");
		close.Pressed.Target = Close;
	}

    private void AddSetting(
		UIBuilder ui,
		ModConfigurationKey key)
	{
		if (_config == null)
			return;

		Type type = key.ValueType();
		object? value = GetValueOrDefault(key);

		ui.Style.MinHeight = 32f;
		ui.Style.PreferredHeight = 32f;

		LocaleString label = (LocaleString)key.Name;

		if (type == typeof(bool))
		{
			_boolInputs[key] =
				ui.HorizontalElementWithLabel(
					label,
					0.7f,
					() => ui.Checkbox(value is bool b && b));

			return;
		}

		_textInputs[key] =
			ui.HorizontalElementWithLabel(
				label,
				0.35f,
				() => ui.TextField(value?.ToString() ?? ""));
	}

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    public void SaveSettings(
        IButton button,
        ButtonEventData eventData)
    {
        if (_mod == null || _config == null)
            return;

        foreach (var pair in _boolInputs)
        {
            _config.Set(pair.Key, pair.Value.IsChecked);
        }

        foreach (var pair in _textInputs)
        {
            ModConfigurationKey key = pair.Key;
            TextField field = pair.Value;

            string text = field.TargetString ?? "";

            try
			{
				object parsed = ParseValue(key.ValueType(), text);
				_config.Set(key, parsed);
			}
			catch (Exception ex)
			{
				UniLog.Warning(
					$"Failed to parse '{key.Name}': {ex.Message}");
			}
        }

        _config.Save();

        Logger.MsgInternal(
            "Saved mod settings: " + _mod.Name);
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    public void Close(
        IButton button,
        ButtonEventData eventData)
    {
        Slot.Destroy();
    }

    private object? GetValueOrDefault(ModConfigurationKey key)
    {
        if (_config == null)
            return null;

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

    private static object ParseValue(Type type, string text)
    {
        return type switch
		{
			_ when type == typeof(string) => text,
			_ when type == typeof(int) => int.Parse(text, CultureInfo.InvariantCulture),
			_ when type == typeof(float) => float.Parse(text, CultureInfo.InvariantCulture),
			_ when type == typeof(double) => double.Parse(text, CultureInfo.InvariantCulture),
			_ when type == typeof(long) => long.Parse(text, CultureInfo.InvariantCulture),
			_ when type == typeof(short) => short.Parse(text, CultureInfo.InvariantCulture),
			_ when type == typeof(byte) => byte.Parse(text, CultureInfo.InvariantCulture),
			_ when type.IsEnum => Enum.Parse(type, text, true),
			_ => text
		};
    }
}