using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace ResoniteModLoader.DataFeedPatches;

[HarmonyPatch(
    typeof(DataFeedItemMapper),
    nameof(DataFeedItemMapper.FindMapping))]
internal static class ModDataFeedGridPatch
{
    private const float DefaultCardWidth = 384f;

    private const float DefaultCardHeight = 128f;

    private const float DefaultSpacing = 12f;

    [HarmonyPrefix]
    private static bool FindMappingPrefix(DataFeedItemMapper __instance,
        DataFeedItem item, ref DataFeedItemMap? __result)
    {
        return item switch
        {
            ModDataFeedGrid gridItem => MapGrid(__instance, gridItem, ref __result),
            DataFeedValueField<colorX> colorField =>MapColorField( __instance, colorField, ref __result),
            ModDataFeedAction card => MapCard(__instance, card, ref __result),
            _ => true
        };
    }

    private static bool MapGrid(
        DataFeedItemMapper mapper,
        ModDataFeedGrid item,
        ref DataFeedItemMap? result)
    {
        DataFeedItemMapper.ItemMapping? mapping =
            mapper.Mappings.FirstOrDefault(candidate =>
                candidate.MatchingType.Value ==
                typeof(ModDataFeedGrid) &&
                candidate.Template.Target != null);

        if (mapping == null)
        {
            mapping = CreateGridMapping(mapper);
        }

        FeedItemInterface? template =
            mapping.Template.Target;

        if (template == null)
        {
            UniLog.Error("[ModDataFeedGridPatch] Grid template creation failed.");
            return true;
        }

        /*
         * Das Template wird wiederverwendet. Deshalb werden die
         * aktuellen Maße unmittelbar vor DataFeedItemMap und damit
         * vor der späteren Instanziierung gesetzt.
         */
        GridLayout? layout = template.Slot.GetComponent<GridLayout>();

        if (layout != null)
        {
            layout.CellSize.Value = new float2(
                    Clamp(
                        item.CardWidth,
                        220f,
                        640f,
                        DefaultCardWidth),

                    Clamp(
                        item.CardHeight,
                        80f,
                        260f,
                        DefaultCardHeight));

            float spacing =
                Clamp(
                    item.Spacing,
                    0f,
                    40f,
                    DefaultSpacing);

            layout.Spacing.Value =
                new float2(
                    spacing,
                    spacing);
        }

        result = new DataFeedItemMap(template);

        return false;
    }

    private static bool MapCard(
        DataFeedItemMapper mapper,
        ModDataFeedAction item,
        ref DataFeedItemMap? result)
    {
        DataFeedItemMapper.ItemMapping? mapping =
            mapper.Mappings.FirstOrDefault(
                candidate =>
                    candidate.MatchingType.Value ==
                    typeof(ModDataFeedAction) &&
                    candidate.Template.Target != null);

        if (mapping == null)
        {
            mapping = CreateCardMapping(mapper);
        }

        FeedItemInterface? template = mapping.Template.Target;

        if (template == null)
        {
            UniLog.Error(
                "[ModDataFeedGridPatch] Card template creation failed.");

            return true;
        }

        Button? button =
            template.Slot.GetComponentInChildren<
                Button>();

        ModSettings? settings =
            Settings.GetActiveSetting<ModSettings>();

        if (button != null)
        {
            bool grid = settings?.GridView.Value ?? true;

            if (settings != null)
            {
                Image? background = button.Slot.GetComponent<Image>();

                if (background != null)
                {
                    background.Tint.Value = settings.ButtonColor.Value;
                }

                if (button.ColorDrivers.Count > 0)
                {
                    var driver = button.ColorDrivers[0];

                    driver.NormalColor.Value = settings.ButtonColor.Value;

                    driver.HighlightColor.Value = settings.ButtonHoverColor.Value;

                    driver.PressColor.Value = settings.ButtonPressedColor.Value;
                }
            }

            button.Label.Size.Value = item.TitleSize;

            if (settings != null)
            {
                button.Label.Color.Value = settings.ButtonTextColor.Value;

                ButtonTextColorDriver? driver = button.Slot.GetComponent<ButtonTextColorDriver>();

                if (driver == null)
                {
                    driver = button.Slot.AttachComponent<ButtonTextColorDriver>();
                }

                driver.Button.Target = button;

                driver.Label.Target = button.Label;

                driver.NormalColor.Value = settings.ButtonTextColor.Value;

                driver.HoverColor.Value = settings.ButtonTextHoverColor.Value;

                driver.PressedColor.Value = settings.ButtonTextPressedColor.Value;
            }

            if (grid)
            {
                button.Label.HorizontalAutoSize.Value = true;
                button.Label.VerticalAutoSize.Value = true;

                button.Label.Align = Alignment.MiddleCenter;
            }
            else
            {
                button.Label.HorizontalAutoSize.Value = false;
                button.Label.VerticalAutoSize.Value = false;

                button.Label.Align = Alignment.MiddleLeft;

                LayoutElement? element =
                    button.Slot.GetComponent<LayoutElement>();

                if (element != null)
                {
                    float height = MathX.Max(item.TitleSize + 24f, 48f);

                    element.MinHeight.Value = height;

                    element.PreferredHeight.Value = height;
                }
            }
        }

        result = new DataFeedItemMap(template);

        return false;
    }

    private static bool MapColorField(
        DataFeedItemMapper mapper,
        DataFeedValueField<colorX> item,
        ref DataFeedItemMap? result)
    {
        DataFeedItemMapper.ItemMapping? mapping =
            mapper.Mappings.FirstOrDefault(x =>
                    x.MatchingType.Value ==
                    typeof(DataFeedValueField<colorX>) &&
                    x.Template.Target != null);

        if (mapping == null)
        {
            mapping = CreateColorFieldMapping(mapper);
        }

        FeedItemInterface? template = mapping.Template.Target;

        if (template == null)
        {
            return true;
        }

        result = new DataFeedItemMap(template);

        return false;
    }

    private static DataFeedItemMapper.ItemMapping
        CreateColorFieldMapping(DataFeedItemMapper mapper)
    {
        DataFeedItemMapper.ItemMapping mapping = mapper.Mappings.Add();

        mapping.MatchingType.Value = typeof(DataFeedValueField<colorX>);

        Slot templateSlot = mapper.Slot.AddSlot("ColorField");

        templateSlot.ActiveSelf = false;

        UIBuilder ui = new(templateSlot);

        RadiantUI_Constants.SetupBaseStyle(ui);

        ui.ForceNext = templateSlot.AttachComponent<RectTransform>();

        ui.VerticalLayout(spacing: 8f, padding: 8f);

        Text label = ui.Text(
                (LocaleString)"Color",
                bestFit: false,
                alignment: Alignment.MiddleLeft);

        label.Size.Value = 24f;

        label.HorizontalAutoSize.Value = false;
        label.VerticalAutoSize.Value = false;

        NativeColorEditor editor = templateSlot.AttachComponent<NativeColorEditor>();

        editor.Build(ui);

        LayoutElement layout = templateSlot.AttachComponent<LayoutElement>();

        layout.MinHeight.Value = 72f;

        layout.PreferredHeight.Value = 72f;

        FeedValueFieldInterface<colorX> feedInterface =
            templateSlot.AttachComponent<FeedValueFieldInterface<colorX>>();

        feedInterface.ItemName.Target = label.Content;

        feedInterface.Value.Target = editor.Value;

        mapping.Template.Target = feedInterface;

        return mapping;
    }

    private static DataFeedItemMapper.ItemMapping
        CreateGridMapping(DataFeedItemMapper mapper)
    {
        DataFeedItemMapper.ItemMapping mapping = mapper.Mappings.Add();

        mapping.MatchingType.Value = typeof(ModDataFeedGrid);

        Slot templateSlot =  mapper.Slot.AddSlot("ModDataFeedGrid");

        templateSlot.ActiveSelf = false;

        UIBuilder ui = new(templateSlot);

        RadiantUI_Constants.SetupBaseStyle(ui);

        ui.ForceNext = templateSlot.AttachComponent<RectTransform>();

        GridLayout layout = ui.GridLayout(
                new float2(
                    DefaultCardWidth,
                    DefaultCardHeight),

                new float2(
                    DefaultSpacing,
                    DefaultSpacing),

                Alignment.TopLeft);

        layout.PaddingLeft.Value = 8f;

        layout.PaddingRight.Value = 8f;

        layout.PaddingTop.Value = 8f;

        layout.PaddingBottom.Value = 8f;

        layout.HorizontalAlign.Value = LayoutHorizontalAlignment.Left;

        layout.VerticalAlign.Value = LayoutVerticalAlignment.Top;

        Text label = ui.Text(
                (LocaleString)"Label",
                bestFit: false,
                alignment: Alignment.MiddleLeft);

        label.Size.Value = 32f;

        label.Color.Value = RadiantUI_Constants.LABEL_COLOR;

        FeedGridInterface feedGridInterface =
            templateSlot.AttachComponent<FeedGridInterface>();

        feedGridInterface.ItemName.Target = label.Content;

        feedGridInterface.ChildContainer.Target = templateSlot;

        mapping.Template.Target = feedGridInterface;

        return mapping;
    }

    private static DataFeedItemMapper.ItemMapping
        CreateCardMapping(
            DataFeedItemMapper mapper)
    {
        DataFeedItemMapper.ItemMapping mapping = mapper.Mappings.Add();

        mapping.MatchingType.Value = typeof(ModDataFeedAction);

        Slot templateSlot = mapper.Slot.AddSlot("ModDataFeedAction");

        templateSlot.ActiveSelf = false;

        UIBuilder ui = new(templateSlot);

        RadiantUI_Constants.SetupBaseStyle(ui);

        ModSettings? settings = Settings.GetActiveSetting<ModSettings>();

        bool grid = settings?.GridView.Value ?? true;

        ui.ForceNext = templateSlot.AttachComponent<RectTransform>();

        if (grid)
        {
            ui.VerticalLayout(
                spacing: 4f,
                padding: 8f,
                childAlignment: Alignment.MiddleCenter,
                forceExpandWidth: true,
                forceExpandHeight: true);
        }
        else
        {
            ui.HorizontalLayout(
                spacing: 24f,
                padding: 8f,
                childAlignment: Alignment.MiddleLeft);
        }

        ui.Style.MinHeight = grid ? 32f : 56f;

        ui.Style.PreferredHeight = ui.Style.MinHeight;

        Button button = ui.Button((LocaleString)"");

        if (!grid)
        {
            button.Label.RectTransform.AddFixedPadding(
                left: 12f,
                top: 0f,
                right: 8f,
                bottom: 0f);
        }

        Image? background = button.Slot.GetComponent<Image>();

        if (background != null)
        {
            background.Tint.Value = RadiantUI_Constants.Neutrals.MIDLIGHT;
        }

        button.Label.Size.Value =
            settings?.TitleSize.Value ?? 26f;

        ButtonValueActionTrigger<string> trigger =
            button.Slot.AttachComponent<
                ButtonValueActionTrigger<string>>();

        FeedValueActionInterface<string> feedInterface =
            templateSlot.AttachComponent<
                FeedValueActionInterface<string>>();

        feedInterface.ItemName.Target = button.Label.Content;

        feedInterface.Action.Target = trigger.OnPressed;

        feedInterface.Value.Target = trigger.Value;

        mapping.Template.Target = feedInterface;

        return mapping;
    }

    private static float Clamp(
        float value,
        float minimum,
        float maximum,
        float fallback)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value) ||
            value <= 0f)
        {
            return fallback;
        }

        return MathX.Clamp(value, minimum, maximum);
    }
}