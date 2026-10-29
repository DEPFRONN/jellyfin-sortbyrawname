using MediaBrowser.Common.Plugins;

namespace SortByRawName;

public class Plugin : BasePlugin
{
    public static readonly Guid PluginId = new("6f4c2b8a-1d57-4e3a-9c84-0b7e5a2d6f19");

    public Plugin()
    {
        // Jellyfin 12.2 reads Version immediately after creating the plugin.
        // BasePlugin leaves it unset, and the plugins page then fails while listing plugins.
        var assembly = typeof(Plugin).Assembly;
        var assemblyPath = assembly.Location;
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        SetAttributes(assemblyPath, directory, assembly.GetName().Version ?? new Version(1, 0, 1, 0));
        SetId(PluginId);
    }

    public override Guid Id => PluginId;

    public override string Name => "SortByRawName";

    public override string Description => "한글 제목을 로마자로 바꾸지 않고 숫자, 영어, 가나다 순으로 정렬합니다.";
}
