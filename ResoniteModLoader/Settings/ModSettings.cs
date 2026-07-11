using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;

namespace ResoniteModLoader;

/// <summary>
/// EXPERIMENTAL:
/// Prototype for a native DataFeed-based mod browser.
/// The goal is to replace the Previous / Next / Open workflow
/// with a searchable native grid/list inside the settings UI.
/// </summary>
[AutoRegisterSetting]
[SettingCategory("ResoniteModLoader")]
public sealed class ModSettings
    : SettingComponent<ModSettings>
{
    public override bool UserspaceOnly => true;

#pragma warning disable CS8618
#pragma warning disable CA1051

    // Diese Einstellung bleibt für die Auswahl
    // zwischen Grid und Liste erhalten.
    [SettingProperty]
    public readonly Sync<bool> GridView;

#pragma warning restore CS8618, CA1051

    // -------------------------------------------------------
    // EXPERIMENTAL
    //
    // Native DataFeed entry point.
    //
    // The engine discovers this method through
    // [SettingSubcategoryEnumerator] and requests
    // DataFeedItems dynamically.
    // -------------------------------------------------------

    [SettingSubcategoryEnumerator(
        "Resonite Mods",
        "All loaded Resonite mods")]
    public async IAsyncEnumerable<DataFeedItem> Mods()
    {
        List<ResoniteModBase> mods =
            GetMods();

        List<DataFeedItem> modItems =
            new();

        foreach (ResoniteModBase mod in mods)
        {
            modItems.Add(
                CreateModButton(mod));
        }

        DataFeedGroup container;

        if (GridView.Value)
        {
            // Wird vom nativen Settings-Mapper
            // als GridLayout dargestellt.
            container =
                new DataFeedGrid();
        }
        else
        {
            // Wird vom nativen Settings-Mapper
            // als vertikale Liste dargestellt.
            container =
                new DataFeedGroup();
        }

        container.InitBase(
            itemKey: "LoadedMods",
            path: null,
            groupingParameters: null,
            label:
                GridView.Value
                    ? $"{mods.Count} Mods"
                    : $"{mods.Count} Mods",
            icon: null,
            setupVisible: null,
            setupEnabled: null,
            subitems: modItems);

        yield return container;

        await Task.CompletedTask;
    }

    // -------------------------------------------------------
    // Creates one DataFeedItem per loaded mod.
    // -------------------------------------------------------

    private static DataFeedValueAction<string>
        CreateModButton(
            ResoniteModBase mod)
    {
        DataFeedValueAction<string> item =
            new();

        string label =
            mod.Name +
            "\n" +
            "★ " +
            mod.Version +
            "\n" +
            mod.Author;

        item.InitBase(
            itemKey:
                "Mod." +
                mod.Name,

            path:
                null,

            groupingParameters:
                null,

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

        item.InitAction(
            setupAction:
                action =>
                    action.ProxySettingAction<
                        ModSettings,
                        string>(
                        nameof(OpenModSettings)),

            value:
                mod.Name);

        return item;
    }

    // -------------------------------------------------------
    // Opens the existing ModConfigurationView for
    // the selected mod.
    // -------------------------------------------------------

    [SyncMethod(
        typeof(Action<string>),
        new string[] { })]
    public void OpenModSettings(
        string modName)
    {
        ResoniteModBase? mod =
            GetMods()
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Name,
                            modName,
                            StringComparison.Ordinal));

        if (mod == null)
        {
            Logger.MsgInternal(
                "Could not find mod: " +
                modName);

            return;
        }

        World world =
            Userspace.UserspaceWorld;

        if (world == null)
            return;

        Slot slot =
            world.AddSlot(
                "Mod Settings - " +
                mod.Name,
                false);

        slot.PositionInFrontOfUser(
            new float3?(
                float3.Backward));

        ModConfigurationView view =
            slot.AttachComponent<
                ModConfigurationView>();

        view.Setup(mod);
    }

    // -------------------------------------------------------
    // Default behaviour.
    // -------------------------------------------------------

    protected override void OnChanges()
    {
        base.OnChanges();

        Logger.MsgInternal(
            "Mod view changed. GridView=" +
            GridView.Value);
    }

    public override void ResetToDefault()
    {
        GridView.Value = true;
    }

    private static List<ResoniteModBase>
        GetMods()
    {
        return ModLoader.Mods()
            .OrderBy(mod => mod.Name)
            .Cast<ResoniteModBase>()
            .ToList();
    }
}