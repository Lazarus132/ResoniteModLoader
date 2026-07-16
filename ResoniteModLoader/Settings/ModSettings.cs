using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

/// <summary>
/// Native DataFeed-based browser for loaded Resonite mods.
/// </summary>
[AutoRegisterSetting]
[SettingCategory("ResoniteModLoader")]
public sealed class ModSettings
    : SettingComponent<ModSettings>
{
    private static readonly Dictionary<
        string,
        ResoniteModBase> modAssemblyMapping =
            new();
    private const float DefaultCardWidth =
        384f;

    private const float DefaultCardHeight =
        128f;

    private const float DefaultCardSpacing =
        12f;

    public override bool UserspaceOnly =>
        true;

    protected override void OnStart()
    {
        base.OnStart();

        modAssemblyMapping.Clear();

        foreach (ResoniteModBase mod in ModLoader.Mods())
        {
            if (mod.ModAssembly == null)
                continue;

            string key =
                Path.GetFileNameWithoutExtension(
                    mod.ModAssembly.File);

            modAssemblyMapping[key] =
                mod;
        }
    }

    #pragma warning disable CS8618
    #pragma warning disable CA1051

    [SettingProperty]
    public readonly Sync<bool> GridView;

    /*
     * Kein SettingProperty:
     * Das Suchfeld erscheint ausschließlich direkt im
     * Mod-Browser und nicht zusätzlich in der allgemeinen
     * ResoniteModLoader-Settings-Liste.
     */
    [SettingProperty(
        visibility: nameof(GridView))]
    [Range(
        220f,
        640f,
        "0")]
    public readonly Sync<float> ModCardWidth;

    [SettingProperty(
        visibility: nameof(GridView))]
    [Range(
        80f,
        260f,
        "0")]
    public readonly Sync<float> ModCardHeight;

    [SettingProperty(
        visibility: nameof(GridView))]
    [Range(
        0f,
        40f,
        "0")]
    public readonly Sync<float> ModCardSpacing;

    [SettingProperty()]
    [Range(
        12f,
        36f,
        "0")]
    public readonly Sync<float> TitleSize;

    [SettingProperty]
    public readonly Sync<colorX> ButtonColor;

    [SettingProperty]
    public readonly Sync<colorX> ButtonHoverColor;

    [SettingProperty]
    public readonly Sync<colorX> ButtonPressedColor;

    [SettingProperty]
    public readonly Sync<colorX> ButtonTextColor;

    [SettingProperty]
    public readonly Sync<colorX> ButtonTextHoverColor;
    [SettingProperty]
    public readonly Sync<colorX> ButtonTextPressedColor;

#pragma warning restore CS8618, CA1051

    [SettingSubcategoryEnumerator(
        "Resonite Mods",
        "All loaded Resonite mods")]
    public async IAsyncEnumerable<DataFeedItem> Mods()
    {
        List<ResoniteModBase> mods =
            GetMods();

        string filter =
            ModBrowserSearchContext.CurrentSearch
            ?? string.Empty;

        List<ResoniteModBase> filteredMods =
            mods
                .Where(mod =>
                    string.IsNullOrWhiteSpace(filter)
                    || mod.Name.Contains(
                        filter,
                        StringComparison.OrdinalIgnoreCase)
                    || mod.Author.Contains(
                        filter,
                        StringComparison.OrdinalIgnoreCase)
                    || mod.Version.Contains(
                        filter,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        DataFeedGroup container;

        if (GridView.Value)
        {
            container =
                new ModDataFeedGrid
                {
                    CardWidth =
                        ModCardWidth.Value,

                    CardHeight =
                        ModCardHeight.Value,

                    Spacing =
                        ModCardSpacing.Value
                };
        }
        else
        {
            container =
                new DataFeedGroup();
        }

        container.InitBase(
            itemKey:
                "LoadedMods",

            path:
                null,

            groupingParameters:
                null,

            label:
                string.IsNullOrWhiteSpace(filter)
                    ? $"{mods.Count} Mods"
                    : $"{filteredMods.Count} of {mods.Count} Mods",

            icon:
                null,

            setupVisible:
                null,

            setupEnabled:
                null,

            subitems:
                null);

        yield return container;

        string[] grouping =
        {
            container.ItemKey
        };

        foreach (ResoniteModBase mod in filteredMods)
        {
            yield return CreateModButton(
                mod,
                grouping);
        }

        await Task.CompletedTask;
    }

    private DataFeedValueAction<string>
        CreateModButton(
            ResoniteModBase mod,
            string[] grouping)
    {
        DataFeedValueAction<string> item;

        item =
            new ModDataFeedAction
            {
                TitleSize =
                    TitleSize.Value,
            };

        string label;

        if (GridView.Value)
        {
            label =
                mod.Name +
                "\n" +
                "★ " +
                mod.Version +
                "\n" +
                mod.Author;
        }
        else
        {
            label =
                mod.Name.PadRight(36) +
                "★ " +
                mod.Version.PadRight(12) +
                mod.Author;
        }

        item.InitBase(
            itemKey:
                "Mod." +
                mod.Name,

            path:
                null,

            groupingParameters:
                grouping,

            label:
                label,

            description:
                "Open settings for " +
                mod.Name,

            icon:
                null,

            setupVisible:
                null,

            setupEnabled:
                null,

            subitems:
                null,

            customEntity:
                mod);

        string key =
            Path.GetFileNameWithoutExtension(
                mod.ModAssembly!.File);

        item.InitAction(
            setupAction:
                action =>
                    action.ProxySettingAction<
                        ModSettings,
                        string>(
                        nameof(OpenModSettings)),

            value:
                key);

        return item;
    }

    [SyncMethod(
        typeof(Action<string>),
        new string[] { })]
    public void OpenModSettings(
        string modKey)
    {
        if (!modAssemblyMapping.TryGetValue(
                modKey,
                out ResoniteModBase? mod))
        {
            Logger.MsgInternal(
                "Could not find mod: " +
                modKey);

            return;
        }

        var dash =
            Userspace.Current.World
                .GetRadiantDash();

        if (dash == null)
        {
            Logger.MsgInternal(
                "Radiant Dash not found.");

            return;
        }

        dash.Open = true;

        RectTransform rect =
            dash.Slot.OpenModalOverlay(
                new float2(
                    0.6f,
                    0.85f),
                $"{mod.Name}    v{mod.Version}");

        Logger.MsgInternal("===== Modal Root =====");

        foreach (Slot child in rect.Slot.Children)
        {
            Logger.MsgInternal(
                child.Name +
                "  Components: " +
                string.Join(
                    ", ",
                    child.Components.Select(
                        c => c.GetType().Name)));
        }

        ModConfigurationView view =
            rect.Slot.AttachComponent<
                ModConfigurationView>();

        view.Setup(mod);
    }

    protected override void OnChanges()
    {
        base.OnChanges();

        /*
         * Ungültige oder noch nicht initialisierte Werte
         * korrigieren. Insbesondere alte gespeicherte Settings
         * besitzen für neue Felder zunächst häufig den Wert 0.
         */
        if (ModCardWidth.Value < 220f)
        {
            ModCardWidth.Value =
                DefaultCardWidth;
        }

        if (ModCardHeight.Value < 80f)
        {
            ModCardHeight.Value =
                DefaultCardHeight;
        }

        if (ModCardSpacing.Value < 0f)
        {
            ModCardSpacing.Value =
                DefaultCardSpacing;
        }

        Logger.MsgInternal(
            $"Grid={GridView.Value}, " +
            $"Card={ModCardWidth.Value:0}x" +
            $"{ModCardHeight.Value:0}, " +
            $"Spacing={ModCardSpacing.Value:0}");
    }

    public override void ResetToDefault()
    {
        GridView.Value =
            true;

        ModCardWidth.Value =
            DefaultCardWidth;

        ModCardHeight.Value =
            DefaultCardHeight;

        ModCardSpacing.Value =
            DefaultCardSpacing;

        ButtonColor.Value =
            RadiantUI_Constants.Neutrals.MIDLIGHT;

        ButtonHoverColor.Value =
            RadiantUI_Constants.Neutrals.LIGHT;

        ButtonPressedColor.Value =
            RadiantUI_Constants.Neutrals.DARK;

        ButtonTextColor.Value =
            colorX.White;

        ButtonTextHoverColor.Value =
            new colorX(
                1f,
                1f,
                0.75f,
                1f);

        ButtonTextPressedColor.Value =
            new colorX(
                0.75f,
                1f,
                1f,
                1f);
    }

    private static List<ResoniteModBase>
        GetMods()
    {
        return ModLoader.Mods()
            .OrderBy(
                mod =>
                    mod.Name)
            .Cast<ResoniteModBase>()
            .ToList();
    }
}