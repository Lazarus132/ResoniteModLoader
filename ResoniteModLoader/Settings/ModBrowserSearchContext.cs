using System;

namespace ResoniteModLoader;

public static class ModBrowserSearchContext
{
    private static string? _currentSearch;

    public static string? CurrentSearch
    {
        get => _currentSearch;

        set
        {
            string? trimmed = value?.Trim();

            _currentSearch = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }

    public static void Clear()
    {
        _currentSearch = null;
    }
}