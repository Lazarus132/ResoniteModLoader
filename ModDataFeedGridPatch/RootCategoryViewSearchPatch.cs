using System;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace ResoniteModLoader.DataFeedPatches;

[HarmonyPatch]
internal static class RootCategoryViewSearchPatch
{
    private const string WrapperName = "RML Persistent Mod Browser Root";

    private const string SearchRootName = "RML Persistent Mod Search";

    /*
     * SetupTemplateLayout wird absichtlich NICHT mehr gepatcht.
     *
     * Das vorhandene Settings-Template kann bereits aus dem
     * gespeicherten Facet geladen worden sein. In diesem Fall wird
     * SetupTemplateLayout überhaupt nicht aufgerufen.
     *
     * OnFeedGenerationStarted läuft dagegen bei jeder Feed-Generation
     * und ist deshalb der zuverlässige Einstiegspunkt.
     */
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(RootCategoryView),
        "OnFeedGenerationStarted")]
    private static void OnFeedGenerationStartedPostfix(
        RootCategoryView __instance,
        IReadOnlyList<string> path)
    {
        bool isModBrowser = IsModBrowserPath(path);

        /*
        * Ganz wichtig:
        *
        * Fremde RootCategoryViews dürfen niemals strukturell
        * verändert werden. Insbesondere die linke Kategorienavigation
        * besitzt ebenfalls eine RootCategoryView.
        */
        if (!isModBrowser)
        {
            RemovePersistentSearchWrapper(__instance);

            return;
        }

        Slot? searchRoot = EnsurePersistentSearchRoot(__instance);

        if (searchRoot == null || searchRoot.IsDestroyed)
        {
            return;
        }

        searchRoot.ActiveSelf = true;
    }

    private static void RemovePersistentSearchWrapper(
        RootCategoryView view)
    {
        Slot? itemsRoot = view.ItemsManager.ContainerRoot.Target;

        if (itemsRoot == null || itemsRoot.IsDestroyed)
        {
            return;
        }

        Slot? wrapper = itemsRoot.Parent;

        if (wrapper == null || wrapper.IsDestroyed ||
            !string.Equals(wrapper.Name, WrapperName, StringComparison.Ordinal))
        {
            return;
        }

        Slot? originalParent = wrapper.Parent;

        if (originalParent == null || originalParent.IsDestroyed)
        {
            return;
        }

        long originalOrderOffset = wrapper.OrderOffset;

        /*
        * Den nativen Feed-Container wieder direkt unter seinen
        * ursprünglichen ScrollContent-Host verschieben.
        */
        itemsRoot.SetParent(originalParent, keepGlobalTransform: false);

        itemsRoot.SetIdentityTransform();

        itemsRoot.OrderOffset = originalOrderOffset;

        /*
        * Diese zusätzliche Einrückung war nur für die Wrapper-Lösung
        * relevant und darf auf normalen Settings-Seiten nicht bleiben.
        */
        VerticalLayout? itemsLayout = itemsRoot.GetComponent<VerticalLayout>();

        if (itemsLayout != null)
        {
            itemsLayout.PaddingTop.Value = 0f;
        }

        /*
        * Der Wrapper enthält jetzt nur noch das Suchfeld und kann
        * vollständig entfernt werden.
        */
        wrapper.Destroy();

        UniLog.Log(
            "[RML] Persistent mod-browser wrapper removed.");
    }

    /*
     * Erzeugt bei Bedarf folgende Struktur:
     *
     * bisher:
     *
     * ScrollContent
     * └─ ItemsManager.ContainerRoot
     *
     * danach:
     *
     * ScrollContent
     * └─ RML Persistent Mod Browser Root
     *    ├─ RML Persistent Mod Search
     *    └─ ItemsManager.ContainerRoot
     *
     * ItemsManager.ContainerRoot bleibt dasselbe Slot-Objekt.
     * Es wird lediglich unter den persistenten Wrapper verschoben.
     *
     * BeginNewGeneration() löscht weiterhin nur die Kinder von
     * ItemsManager.ContainerRoot. Wrapper und Suchfeld bleiben bestehen.
     */
    private static Slot? EnsurePersistentSearchRoot(
        RootCategoryView view)
    {
        Slot? itemsRoot = view.ItemsManager.ContainerRoot.Target;

        if (itemsRoot == null || itemsRoot.IsDestroyed)
        {
            UniLog.Warning("[RML] ItemsManager.ContainerRoot is unavailable.");

            return null;
        }

        /*
         * Bereits vollständig umgebaut:
         *
         * Wrapper
         * ├─ SearchRoot
         * └─ ItemsRoot
         */
        Slot? currentParent = itemsRoot.Parent;

        if (currentParent != null &&
            !currentParent.IsDestroyed &&
            string.Equals(
                currentParent.Name,
                WrapperName,
                StringComparison.Ordinal))
        {
            Slot? existingSearch = FindDirectChild(currentParent, SearchRootName);

            if (existingSearch != null)
            {
                return existingSearch;
            }

            return CreateSearchRoot(view, currentParent);
        }

        /*
         * Das ist der ursprüngliche ScrollArea-Content-Host.
         */
        Slot? originalParent = itemsRoot.Parent;

        if (originalParent == null || originalParent.IsDestroyed)
        {
            UniLog.Warning(
                "[RML] Parent of ItemsManager.ContainerRoot "
                + "is unavailable.");

            return null;
        }

        /*
         * Falls ein früherer Patchlauf den Wrapper bereits als
         * Geschwister erzeugt hat, aber ItemsRoot noch nicht darunter
         * verschoben wurde, verwenden wir diesen Wrapper weiter.
         */
        Slot? wrapper =
            FindDirectChild(originalParent, WrapperName);

        if (wrapper == null || wrapper.IsDestroyed)
        {
            wrapper = originalParent.AddSlot(WrapperName);

            wrapper.OrderOffset = itemsRoot.OrderOffset;

            UIBuilder wrapperUi = new(wrapper);

            RadiantUI_Constants.SetupBaseStyle(wrapperUi);

            wrapperUi.VerticalLayout(
                spacing: 10f,
                padding: 0f, Alignment.TopLeft,
                forceExpandWidth: true,
                forceExpandHeight: false);

            wrapperUi.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
        }

        if (itemsRoot.Parent != wrapper)
        {
            itemsRoot.SetParent(wrapper, keepGlobalTransform: false);
            itemsRoot.SetIdentityTransform();
        }

        VerticalLayout? layout = itemsRoot.GetComponent<VerticalLayout>();

        if (layout != null)
        {
            layout.PaddingTop.Value = 96f;
        }

        /*
         * Das Suchfeld muss vor dem Feed-Container stehen.
         */
        Slot? searchRoot = FindDirectChild(wrapper, SearchRootName);

        if (searchRoot == null || searchRoot.IsDestroyed)
        {
            searchRoot = CreateSearchRoot(view, wrapper);
        }

        if (searchRoot == null)
        {
            return null;
        }

        searchRoot.OrderOffset = long.MinValue;

        return searchRoot;
    }

    private static Slot? CreateSearchRoot(
        RootCategoryView view,
        Slot wrapper)
    {
        if (wrapper.IsDestroyed)
        {
            return null;
        }

        Slot searchRoot = wrapper.AddSlot(SearchRootName);

        searchRoot.OrderOffset = long.MinValue;

        UIBuilder ui =new(searchRoot);

        RadiantUI_Constants.SetupBaseStyle(ui);

        ui.VerticalLayout(
            spacing: 4f,

            padding: 8f,

            childAlignment: Alignment.MiddleLeft,

            forceExpandWidth: true,

            forceExpandHeight: false);

        /*
         * Gesamthöhe des Suchblocks.
         */
        ui.Style.MinHeight = 84f;

        ui.Style.PreferredHeight = 84f;

        Text title = ui.Text(
                (LocaleString)"Search Mods",
                bestFit: false,
                alignment: Alignment.MiddleLeft);

        title.Size.Value = 20f;

        /*
         * Höhe des eigentlichen Eingabefeldes.
         */
        ui.Style.MinHeight = 48f;

        ui.Style.PreferredHeight = 48f;

        float oldMin = ui.Style.TextAutoSizeMin;

        float oldMax = ui.Style.TextAutoSizeMax;

        ui.Style.TextAutoSizeMin = 15f;
        ui.Style.TextAutoSizeMax = 24f;

        TextField searchField =
            ui.TextField(
                defaultText: ModBrowserSearchContext.CurrentSearch,
                undo: false,
                undoDescription: null,
                parseRTF: false,
                promptText: default);

        Slot placeholderSlot = searchField.Slot.AddSlot("RML Search Placeholder");

        placeholderSlot.OrderOffset = long.MaxValue;

        Text placeholder = placeholderSlot.AttachComponent<Text>();

        placeholder.Content.Value = "Type to search mods...";

        placeholder.ParseRichText.Value = false;

        placeholder.Color.Value =
            new colorX(
                1f,
                1f,
                1f,
                0.35f);

        placeholder.Align = Alignment.MiddleLeft;

        placeholder.Size.Value = 20f;

        placeholder.HorizontalAutoSize.Value = false;

        placeholder.VerticalAutoSize.Value = false;

        placeholder.RectTransform.AnchorMin.Value = float2.Zero;

        placeholder.RectTransform.AnchorMax.Value = float2.One;

        placeholder.RectTransform.OffsetMin.Value =
            new float2(
                12f,
                0f);

        placeholder.RectTransform.OffsetMax.Value =
            new float2(
                -12f,
                0f);

        ui.Style.TextAutoSizeMin =  oldMin;

        ui.Style.TextAutoSizeMax = oldMax;

        ModBrowserSearchDriver searchDriver =
            searchRoot.AttachComponent<ModBrowserSearchDriver>();

        searchDriver.Placeholder.Target = placeholderSlot;

        searchDriver.SearchPhrase.Value =
            ModBrowserSearchContext.CurrentSearch ?? string.Empty;

        searchDriver.SearchPhrase.DriveFrom(searchField.TargetStringField);

        searchDriver.TargetView.Target = view;

        placeholderSlot.ActiveSelf =
            string.IsNullOrWhiteSpace(ModBrowserSearchContext.CurrentSearch);

        searchRoot.ActiveSelf = false;

        return searchRoot;
    }

    /*
     * Sucht oberhalb des ursprünglichen Hosts nach einer ScrollRect,
     * deren Content noch auf ItemsRoot zeigt, und richtet sie auf den
     * neuen Wrapper um.
     *
     * Damit wird nicht nur das Feed-Root, sondern auch das Suchfeld
     * Bestandteil des tatsächlich sichtbaren Scroll-Inhalts.
     */

    private static Slot? FindDirectChild(Slot parent, string name)
    {
        foreach (Slot child in parent.Children)
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static bool IsModBrowserPath(IReadOnlyList<string>? path)
    {
        if (path == null)
        {
            return false;
        }

        string expectedSegment =
            nameof(ModSettings)
            + "."
            + nameof(ModSettings.Mods);

        foreach (string segment in path)
        {
            if (string.Equals(
                    segment,
                    expectedSegment,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}