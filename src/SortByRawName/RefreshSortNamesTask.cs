using System.Diagnostics;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SortByRawName;

/// <summary>
/// Rewrites the stored SortName column. Jellyfin 12 sorts library lists from that column,
/// and values written before this plugin was installed are already romanized.
/// </summary>
public class RefreshSortNamesTask : IScheduledTask
{
    private const int BatchSize = 200;

    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbFactory;
    private readonly ILogger<RefreshSortNamesTask> _logger;

    public RefreshSortNamesTask(
        ILibraryManager libraryManager,
        IDbContextFactory<JellyfinDbContext> dbFactory,
        ILogger<RefreshSortNamesTask> logger)
    {
        _libraryManager = libraryManager;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public string Name => "원문 제목으로 정렬 다시 적용";

    public string Key => "SortByRawNameRefresh";

    public string Description => "한글 제목을 로마자(A, B, C)로 둔 정렬 값을 지우고, 숫자·영어·가나다 순이 되도록 다시 저장합니다.";

    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.StartupTrigger
        };
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (!await Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("정렬 제목 다시 계산이 이미 실행 중입니다.");
            return;
        }

        var started = Stopwatch.StartNew();
        try
        {
            TransliteratorDisabler.Disable();
            if (!HangulStaysHangul())
            {
                _logger.LogError(
                    "한글 제목이 아직 로마자로 바뀝니다. 정렬 제목은 건드리지 않았습니다. 플러그인을 켠 뒤 젤리핀을 다시 시작하세요.");
                return;
            }

            progress.Report(0);
            var updates = CollectUpdates(cancellationToken);
            _logger.LogInformation("정렬 제목을 바꿀 항목 {Count}개를 찾았습니다.", updates.Count);
            progress.Report(30);

            var written = 0;
            for (var offset = 0; offset < updates.Count; offset += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = updates.Skip(offset).Take(BatchSize).ToList();
                written += await WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                var done = Math.Min(updates.Count, offset + BatchSize);
                progress.Report(30 + (70d * done / Math.Max(updates.Count, 1)));
            }

            _logger.LogInformation(
                "정렬 제목 {Count}개를 원문으로 다시 저장했습니다. 걸린 시간: {Elapsed}.",
                written,
                started.Elapsed);
            progress.Report(100);
        }
        finally
        {
            Gate.Release();
        }
    }

    private List<(Guid Id, string? SortName)> CollectUpdates(CancellationToken cancellationToken)
    {
        var root = _libraryManager.GetUserRootFolder();
        var seen = new HashSet<Guid>();
        var updates = new List<(Guid Id, string? SortName)>();
        var samplesLogged = 0;

        foreach (var library in root.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Consider(library);

            var children = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ParentId = library.Id,
                Recursive = true
            });

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Consider(child);
            }
        }

        return updates;

        void Consider(BaseItem item)
        {
            if (!seen.Add(item.Id) || item.Id.Equals(Guid.Empty))
            {
                return;
            }

            var cached = _libraryManager.GetItemById(item.Id) ?? item;
            var previous = cached.SortName;
            cached.SortName = null!;
            var rebuilt = cached.SortName;
            if (string.Equals(previous, rebuilt, StringComparison.Ordinal))
            {
                return;
            }

            updates.Add((cached.Id, rebuilt));
            if (samplesLogged < 8)
            {
                samplesLogged++;
                _logger.LogInformation("정렬 제목 변경: '{Name}' [{Previous}] -> [{Rebuilt}]", cached.Name, previous, rebuilt);
            }
        }
    }

    private static bool HangulStaysHangul()
    {
        var configuration = BaseItem.ConfigurationManager?.Configuration ?? new ServerConfiguration();
        var sortName = BaseItem.GetSortName("아바타", true, configuration);
        return sortName is not null && sortName.Contains('아');
    }

    private async Task<int> WriteBatchAsync(IReadOnlyList<(Guid Id, string? SortName)> batch, CancellationToken cancellationToken)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (id, sortName) in batch)
        {
            var writtenName = sortName;
            await context.BaseItems
                .Where(entity => entity.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.SortName, writtenName), cancellationToken)
                .ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return batch.Count;
    }
}
