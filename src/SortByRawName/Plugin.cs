using MediaBrowser.Common.Plugins;

namespace SortByRawName;

public class Plugin : BasePlugin
{
    public override string Name => "SortByRawName";

    public override string Description => "한글 제목을 로마자로 바꾸지 않고 숫자, 영어, 가나다 순으로 정렬합니다.";
}
