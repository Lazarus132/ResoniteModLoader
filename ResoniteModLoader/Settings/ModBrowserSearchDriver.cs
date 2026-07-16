using System;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;

namespace ResoniteModLoader;

/// <summary>
/// Verwaltet die lokale Suche des Mod-Browsers.
///
/// Die native RootCategoryView.SearchPhrase wird nicht verwendet,
/// weil diese auch die linke Settings-Navigation filtert.
/// </summary>
public sealed class ModBrowserSearchDriver
    : Component
{
    public readonly SyncRef<Slot> Placeholder;
    private static readonly MethodInfo?
        ForceRegenerateMethod = AccessTools.Method(
                typeof(DataFeedViewBase), "ForceRegenerate");

    #pragma warning disable CS8618
    #pragma warning disable CA1051

    /*
     * Dieses eigene Sync-Feld wird direkt vom TextField getrieben.
     *
     * Weil es zum Driver selbst gehört, wird OnChanges zuverlässig
     * aufgerufen, sobald sich der eingegebene Suchtext ändert.
     */
    public readonly Sync<string> SearchPhrase;

    public readonly SyncRef<RootCategoryView> TargetView;

    #pragma warning restore CS8618, CA1051

    private string _lastSearch = string.Empty;

    protected override void OnStart()
    {
        base.OnStart();

        _lastSearch = Normalize(SearchPhrase.Value);

        ModBrowserSearchContext.CurrentSearch =
            string.IsNullOrWhiteSpace(_lastSearch) ? null : _lastSearch;

        Slot? placeholder = Placeholder.Target;

        if (placeholder != null &&!placeholder.IsDestroyed)
        {
            placeholder.ActiveSelf = string.IsNullOrWhiteSpace(_lastSearch);
        }
    }

    protected override void OnChanges()
    {
        base.OnChanges();

        RootCategoryView? view = TargetView.Target;

        if (view == null || view.IsDestroyed)
        {
            return;
        }

        string nextSearch = Normalize(SearchPhrase.Value);

        /*
        * Die Placeholder-Sichtbarkeit immer aktualisieren,
        * auch wenn der normalisierte Suchwert unverändert ist.
        */
        Slot? placeholder = Placeholder.Target;

        if (placeholder != null && !placeholder.IsDestroyed)
        {
            placeholder.ActiveSelf =
                string.IsNullOrWhiteSpace(nextSearch);
        }

        if (string.Equals(
            nextSearch, _lastSearch,
            StringComparison.Ordinal))
        {
            return;
        }

        _lastSearch = nextSearch;

        ModBrowserSearchContext.CurrentSearch =
            string.IsNullOrWhiteSpace(nextSearch)
                ? null : nextSearch;

        if (ForceRegenerateMethod == null)
        {
            Logger.MsgInternal(
                "[RML] DataFeedViewBase.ForceRegenerate "
                + "was not found.");

            return;
        }

        try
        {
            ForceRegenerateMethod.Invoke(view, null);
        }
        catch (Exception exception)
        {
            Logger.MsgInternal(
                "[RML] Failed to regenerate mod browser:\n"
                + exception);
        }
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}