using EdgeTTS.Models;

namespace EdgeTTS.Dalamud.ViewModels;

/// <summary>
///     音色浏览器的状态与逻辑, 负责搜索过滤、语音选择及副作用清理
/// </summary>
internal sealed class VoiceExplorerViewModel
(
    EdgeTTSEngine   engine,
    EdgeTTSSettings settings,
    Action          save
)
{
    private IReadOnlyList<VoiceInfo>? allVoices;
    private Task<int>?                refreshTask;

    /// <summary>
    ///     搜索查询文本, 由 UI 双向绑定
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    ///     最近一次刷新音色列表的结果说明
    /// </summary>
    public string? RefreshStatus { get; private set; }

    /// <summary>
    ///     是否正在从服务器刷新音色列表
    /// </summary>
    public bool IsRefreshing => refreshTask is { IsCompleted: false };

    /// <summary>
    ///     扁平化的全部语音列表 (懒加载缓存)
    /// </summary>
    public IReadOnlyList<VoiceInfo> AllVoices
    {
        get
        {
            if (allVoices != null)
                return allVoices;

            return allVoices = [.. engine.Voices.Values.SelectMany(g => g.Values).SelectMany(v => v)];
        }
    }

    /// <summary>
    ///     当前选中的音色, 若短名称不匹配则返回 null
    /// </summary>
    public VoiceInfo? SelectedVoice =>
        AllVoices.FirstOrDefault(v => string.Equals(v.ShortName, settings.Voice, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     按搜索词过滤后的分组树: 地区 → (性别, 语音列表)
    /// </summary>
    public IEnumerable<(string Locale, IEnumerable<(string Gender, IReadOnlyList<VoiceInfo> Voices)> Genders)> GetFilteredTree()
    {
        var query   = SearchQuery.Trim();
        var noQuery = string.IsNullOrWhiteSpace(query);

        foreach (var (locale, genders) in engine.Voices)
        {
            var localeMatches   = !noQuery && locale.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredGenders = new List<(string Gender, IReadOnlyList<VoiceInfo> Voices)>();

            foreach (var (gender, voices) in genders)
            {
                VoiceInfo[] matching;

                if (noQuery || localeMatches)
                    matching = voices;
                else
                {
                    matching =
                    [
                        .. voices.Where
                        (v => VoiceDisplayName.Get(v).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              v.ShortName.Contains(query, StringComparison.OrdinalIgnoreCase)           ||
                              v.Gender.Contains(query, StringComparison.OrdinalIgnoreCase)
                        )
                    ];

                    if (matching.Length == 0)
                        continue;
                }

                filteredGenders.Add((gender, matching));
            }

            if (filteredGenders.Count == 0)
                continue;

            yield return (locale, filteredGenders);
        }
    }

    /// <summary>
    ///     发起一次音色列表刷新, 刷新进行中时忽略重复请求
    /// </summary>
    public void RefreshVoices()
    {
        if (IsRefreshing)
            return;

        RefreshStatus = null;
        refreshTask   = engine.RefreshVoicesAsync(CancellationToken.None);
    }

    /// <summary>
    ///     由 UI 每帧调用, 在刷新任务结束时更新状态并作废语音缓存
    /// </summary>
    public void PollRefresh()
    {
        if (refreshTask is not { IsCompleted: true })
            return;

        var finished = refreshTask;
        refreshTask = null;
        allVoices   = null;

        RefreshStatus = finished.IsCompletedSuccessfully ?
                            string.Format(Lang.Get("Window.RefreshVoicesDone"), finished.Result) :
                            $"{Lang.Get("Window.RefreshVoicesFailed")}: {finished.Exception?.GetBaseException().Message}";
    }

    /// <summary>
    ///     选中指定音色, 处理兼容性清理并持久化
    /// </summary>
    public void SelectVoice
    (
        VoiceInfo voice
    )
    {
        settings.Voice = voice.ShortName;
        save();
    }

    /// <summary>
    ///     指定短名称的语音是否为当前选中项
    /// </summary>
    public bool IsSelected
    (
        string shortName
    ) =>
        string.Equals(settings.Voice, shortName, StringComparison.OrdinalIgnoreCase);
}
