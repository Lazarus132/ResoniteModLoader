/*
 * EXPERIMENTAL PROTOTYPE
 *
 * Original standalone mod browser prototype.
 *
 * This implementation creates its own UI panel and
 * is currently kept as a reference while evaluating
 * a native DataFeed-based integration into the
 * Resonite settings system.
 */

using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

[Obsolete(
    "Experimental prototype. " +
    "Currently replaced by the native DataFeed implementation.",
    false)]
public sealed class ModBrowserView : Component
{
    private bool _gridView;

    private TextField? _searchField;

    private readonly List<ModBrowserEntry>
        _entries = new();

    public void Setup(bool gridView)
    {
        _gridView = gridView;

        Build();
    }

    private void Build()
    {
        _entries.Clear();

        List<ResoniteModBase> mods =
            GetMods();

        UIBuilder ui =
            RadiantUI_Panel.SetupPanel(
                Slot,
                "Mod Browser",
                new float2(1200f, 900f),
                pinButton: true);

        Slot.LocalScale *= 0.0005f;

        RadiantUI_Constants.SetupDefaultStyle(ui);

        // Gesamter Browser
        ui.VerticalLayout(
            spacing: 8f,
            padding: 8f,
            childAlignment: Alignment.TopCenter,
            forceExpandWidth: true,
            forceExpandHeight: true);

        // -------------------------
        // Überschrift
        // -------------------------

        ui.Style.MinHeight = 48f;
        ui.Style.PreferredHeight = 48f;

        Text heading =
            ui.Text(
                "Installed Mods",
                bestFit: true,
                alignment: Alignment.MiddleCenter);

        heading.Color.Value =
            colorX.White;

        // -------------------------
        // Anzahl
        // -------------------------

        ui.Style.MinHeight = 28f;
        ui.Style.PreferredHeight = 28f;

        Text countText =
            ui.Text(
                $"{mods.Count} mods loaded",
                bestFit: true,
                alignment: Alignment.MiddleCenter);

        countText.Color.Value =
            colorX.White.SetA(0.7f);

        // -------------------------
        // Suchfeld
        // -------------------------

        ui.Style.MinHeight = 42f;
        ui.Style.PreferredHeight = 42f;

        _searchField =
            ui.TextField(
                "",
                false,
                "",
                false,
                "🔍 Search mods...");

        // Die erste Version verwendet einen Apply-Button.
        // Live-Filterung ergänzen wir danach über TextEditor-Events.

        ui.Style.MinHeight = 36f;
        ui.Style.PreferredHeight = 36f;

        Button applySearch =
            ui.Button(
                (LocaleString)"Apply Search");

        applySearch.Pressed.Target =
            ApplySearch;

        ui.Spacer(8f);

        // -------------------------
        // Scrollbereich
        // -------------------------

        ui.Style.MinHeight = -1f;
        ui.Style.PreferredHeight = -1f;

        ui.ScrollArea(
            Alignment.TopCenter);

        if (_gridView)
        {
            BuildGrid(
                ui,
                mods);

            // GridLayout verlassen
            ui.NestOut();
        }
        else
        {
            BuildList(
                ui,
                mods);

            // VerticalLayout verlassen
            ui.NestOut();
        }

        // ScrollArea verlassen
        ui.NestOut();

        // Äußeres VerticalLayout verlassen
        ui.NestOut();

        Logger.MsgInternal(
            $"Built mod browser with " +
            $"{mods.Count} mods. " +
            $"GridView={_gridView}");
    }

    private void BuildGrid(
        UIBuilder ui,
        IReadOnlyList<ResoniteModBase> mods)
    {
        GridLayout grid =
            ui.GridLayout(
                new float2(
                    220f,
                    120f),
                new float2(
                    16f,
                    16f),
                Alignment.TopCenter);

        grid.PaddingTop.Value = 16f;
        grid.PaddingRight.Value = 16f;
        grid.PaddingBottom.Value = 16f;
        grid.PaddingLeft.Value = 16f;

        grid.HorizontalAlign.Value =
            LayoutHorizontalAlignment.Center;

        grid.VerticalAlign.Value =
            LayoutVerticalAlignment.Top;

        grid.ExpandWidthToFit.Value =
            false;

        grid.PreserveAspectOnExpand.Value =
            false;

        grid.AlignLastRowIndividually.Value =
            true;

        foreach (ResoniteModBase mod in mods)
        {
            CreateModCard(
                ui,
                mod);
        }
    }

    private void BuildList(
        UIBuilder ui,
        IReadOnlyList<ResoniteModBase> mods)
    {
        ui.VerticalLayout(
            8f,
            8f,
            Alignment.TopCenter,
            true,
            false);

        ui.FitContent(
            SizeFit.Disabled,
            SizeFit.PreferredSize);

        foreach (ResoniteModBase mod in mods)
        {
            CreateModCard(
                ui,
                mod);
        }
    }

    private void CreateModCard(
        UIBuilder ui,
        ResoniteModBase mod)
    {
        // In der Listenansicht bekommt jede Zeile mehr Breite.
        if (_gridView)
        {
            ui.Style.MinHeight = 120f;
            ui.Style.PreferredHeight = 120f;
        }
        else
        {
            ui.Style.MinHeight = 96f;
            ui.Style.PreferredHeight = 96f;
        }

        Slot card =
            ui.Empty(
                "Mod - " +
                mod.Name);

        Image background =
            card.AttachComponent<Image>();

        background.Sprite.Target =
            RadiantUI_Constants.GetButtonSprite(
                card.World);

        background.NineSliceSizing.Value =
            NineSliceSizing.RectHeight;

        background.Tint.Value =
            RadiantUI_Constants.Neutrals.MIDLIGHT;

        Button button =
            card.AttachComponent<Button>();

        ModBrowserCard cardController =
            card.AttachComponent<ModBrowserCard>();

        cardController.Setup(mod);

        button.Pressed.Target =
            cardController.OpenModSettings;

        _entries.Add(
            new ModBrowserEntry(
                mod,
                card));

        // -------------------------
        // Innenabstand
        // -------------------------

        Slot content =
            card.AddSlot("Content");

        RectTransform contentRect =
            content.AttachComponent<RectTransform>();

        contentRect.AnchorMin.Value =
            float2.Zero;

        contentRect.AnchorMax.Value =
            float2.One;

        contentRect.OffsetMin.Value =
            new float2(
                8f,
                8f);

        contentRect.OffsetMax.Value =
            new float2(
                -8f,
                -8f);

        // -------------------------
        // Mod-Name
        // -------------------------

        Slot titleSlot =
            content.AddSlot("Title");

        RectTransform titleRect =
            titleSlot.AttachComponent<RectTransform>();

        titleRect.AnchorMin.Value =
            new float2(
                0f,
                0.66f);

        titleRect.AnchorMax.Value =
            new float2(
                1f,
                1f);

        titleRect.OffsetMin.Value =
            float2.Zero;

        titleRect.OffsetMax.Value =
            float2.Zero;

        Text title =
            titleSlot.AttachComponent<Text>();

        title.Content.Value =
            mod.Name;

        title.AutoSize = true;
        title.AutoSizeMin.Value = 6f;
        title.AutoSizeMax.Value = 26f;

        title.Color.Value =
            colorX.White;

        title.HorizontalAlign.Value =
            TextHorizontalAlignment.Center;

        title.VerticalAlign.Value =
            TextVerticalAlignment.Middle;

        // -------------------------
        // Symbol / Version
        // -------------------------

        Slot centerSlot =
            content.AddSlot("Center");

        RectTransform centerRect =
            centerSlot.AttachComponent<RectTransform>();

        centerRect.AnchorMin.Value =
            new float2(
                0f,
                0.25f);

        centerRect.AnchorMax.Value =
            new float2(
                1f,
                0.66f);

        centerRect.OffsetMin.Value =
            float2.Zero;

        centerRect.OffsetMax.Value =
            float2.Zero;

        Text centerText =
            centerSlot.AttachComponent<Text>();

        centerText.Content.Value =
            "★  " +
            mod.Version;

        centerText.AutoSize = true;
        centerText.AutoSizeMin.Value = 6f;
        centerText.AutoSizeMax.Value = 28f;

        centerText.Color.Value =
            colorX.Cyan;

        centerText.HorizontalAlign.Value =
            TextHorizontalAlignment.Center;

        centerText.VerticalAlign.Value =
            TextVerticalAlignment.Middle;

        // -------------------------
        // Autor
        // -------------------------

        Slot authorSlot =
            content.AddSlot("Author");

        RectTransform authorRect =
            authorSlot.AttachComponent<RectTransform>();

        authorRect.AnchorMin.Value =
            new float2(
                0f,
                0f);

        authorRect.AnchorMax.Value =
            new float2(
                1f,
                0.25f);

        authorRect.OffsetMin.Value =
            float2.Zero;

        authorRect.OffsetMax.Value =
            float2.Zero;

        Text author =
            authorSlot.AttachComponent<Text>();

        author.Content.Value =
            mod.Author;

        author.AutoSize = true;
        author.AutoSizeMin.Value = 5f;
        author.AutoSizeMax.Value = 18f;

        author.Color.Value =
            colorX.White.SetA(0.7f);

        author.HorizontalAlign.Value =
            TextHorizontalAlignment.Center;

        author.VerticalAlign.Value =
            TextVerticalAlignment.Middle;
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    public void ApplySearch(
        IButton button,
        ButtonEventData eventData)
    {
        string search =
            _searchField?.TargetString?
                .Trim() ?? "";

        foreach (ModBrowserEntry entry in
                 _entries)
        {
            bool visible =
                string.IsNullOrWhiteSpace(
                    search) ||
                ContainsIgnoreCase(
                    entry.Mod.Name,
                    search) ||
                ContainsIgnoreCase(
                    entry.Mod.Author,
                    search) ||
                ContainsIgnoreCase(
                    entry.Mod.Version,
                    search);

            entry.Slot.ActiveSelf =
                visible;
        }
    }

    private static bool ContainsIgnoreCase(
        string? value,
        string filter)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(
                   filter,
                   StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static List<ResoniteModBase>
        GetMods()
    {
        return ModLoader.Mods()
            .OrderBy(mod => mod.Name)
            .Cast<ResoniteModBase>()
            .ToList();
    }

    private sealed class ModBrowserEntry
    {
        public ResoniteModBase Mod { get; }

        public Slot Slot { get; }

        public ModBrowserEntry(
            ResoniteModBase mod,
            Slot slot)
        {
            Mod = mod;
            Slot = slot;
        }
    }
}

/// <summary>
/// EXPERIMENTAL.
///
/// Controller for a single standalone browser card.
/// Kept as part of the original prototype.
/// </summary>
public sealed class ModBrowserCard : Component
{
    private ResoniteModBase? _mod;

    public void Setup(
        ResoniteModBase mod)
    {
        _mod = mod;
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    public void OpenModSettings(
        IButton button,
        ButtonEventData eventData)
    {
        if (_mod == null)
            return;

        World world =
            Userspace.UserspaceWorld;

        if (world == null)
            return;

        Slot slot =
            world.AddSlot(
                "Mod Settings - " +
                _mod.Name,
                false);

        slot.PositionInFrontOfUser(
            new float3?(
                float3.Backward));

        ModConfigurationView view =
            slot.AttachComponent<
                ModConfigurationView>();

        view.Setup(_mod);
    }
}