using System.Runtime.CompilerServices;
using Jellyfin.Extensions;

namespace SortByRawName;

/// <summary>
/// Disables Jellyfin's ICU transliterator before it is created.
/// An empty id is not the same as an unset variable: unset keeps the Latin default.
/// </summary>
public static class TransliteratorDisabler
{
    public const string VariableName = "JELLYFIN_TRANSLITERATOR_ID";

    [ModuleInitializer]
    internal static void Initialize()
    {
        Disable();

        // Create the transliterator now, while the id is empty. The server caches it
        // on first use, so a later setting would not take effect.
        _ = "아".Transliterated();
    }

    public static void Disable()
    {
        Environment.SetEnvironmentVariable(VariableName, string.Empty);
    }
}
