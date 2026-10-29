using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using SortByRawName;
using Xunit;

namespace SortByRawName.Tests;

public class SortOrderTests
{
    private readonly ServerConfiguration _configuration = new();

    static SortOrderTests()
    {
        TransliteratorDisabler.Disable();
    }

    [Fact]
    public void Hangul_sort_key_is_not_romanized()
    {
        var sortName = BaseItem.GetSortName("아바타", true, _configuration);

        Assert.StartsWith("아", sortName);
        Assert.DoesNotContain("a", sortName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Titles_sort_as_digits_then_latin_then_hangul()
    {
        string[] titles =
        [
            "차이나",
            "banana",
            "아버지",
            "10편",
            "Avatar",
            "가나다",
            "2편",
            "다이하드",
            "나비"
        ];

        var ordered = titles
            .OrderBy(title => BaseItem.GetSortName(title, true, _configuration), StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["2편", "10편", "Avatar", "banana", "가나다", "나비", "다이하드", "아버지", "차이나"],
            ordered);
    }

    [Fact]
    public void Leading_article_is_removed_before_sorting()
    {
        var sortName = BaseItem.GetSortName("The Matrix", true, _configuration);

        Assert.Equal("matrix", sortName);
    }
}
